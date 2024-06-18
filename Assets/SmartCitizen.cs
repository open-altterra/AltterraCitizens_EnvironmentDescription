using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SmartCitizen : MonoBehaviour
{
    [System.Serializable]
    public class StartSessionRequest
    {
        public string provider_type;
        public string person_id;
        public int target_tickrate;
    }

    [System.Serializable]
    public class StartSessionResponse
    {
        public string session_id;
    }

    [System.Serializable]
    public class StopSessionRequest
    {
        public string session_id;
    }

    [System.Serializable]
    public class UpdateStateRequest
    {
        public string session_id;
        public string visual;
        public string external;
        public string episodic;
    }

    [System.Serializable]
    public class GetResponseData
    {
        public List<string> response = new List<string>();
        public string state = "";
    }

    public class Speech
    {
        public Speech(string text, string name)
        {
            Text = text;
            CreatedTime = DateTime.Now;
            Name = name;
        }

        public string Name { get; private set; }
        public string Text { get; private set; }
        public DateTime CreatedTime { get; private set; } = DateTime.Now;
    }

    [field: Header("Reference")]
    [field: SerializeField]
    public ObjectDetector ObjectDetector { get; private set; }

    [field: SerializeField]
    public Text NameUI { get; private set; }

    [field: SerializeField]
    public Text SpeechUI { get; private set; }

    [field: Header("Server settings")]
    [field: SerializeField]
    public string Domain { get; private set; }

    [field: SerializeField]
    public float UpdateDelay { get; private set; } = 5f;

    [field: SerializeField]
    [field: Multiline(30)]
    public string CurrentState { get; private set; } = "";

    [field: Header("Bot settings")]
    [field: SerializeField]
    public string Name { get; private set; } = "Bot";

    [SerializeField]
    [Min(0f)]
    private float speechMemoryTime = 20f;

    [field: SerializeField]
    [field: Multiline(10)]
    public string Visual { get; private set; }

    [field: SerializeField]
    public List<Speech> External { get; private set; } = new List<Speech>();

    [field: SerializeField]
    [field: Multiline(10)]
    public string ExternalText { get; private set; } = string.Empty;

    [field: SerializeField]
    [field: Multiline(10)]
    public string Episodic { get; private set; } = "Вы находитесь в комнате.";

    [SerializeField]
    [Min(0f)]
    private float delay = 0.5f;

    private string sessionId;
    private bool isSessionActive = false;

    private static UnityEvent<string, Speech> AloudSpeech = new UnityEvent<string, Speech>();

    private void Start()
    {
        Episodic = $"Твоё имя {Name}. Ты - говорящий пёс. Находишься в комнате. Общайся с другими.";
        NameUI.text = Name;
        AloudSpeech?.AddListener(HandleAloudSpeech);
        StartCoroutine(DetectLoop());
        StartCoroutine(StartSession());
    }

    private void OnDisable()
    {
        if (isSessionActive)
        {
            StopSession();
        }
    }

    private void HandleAloudSpeech(string name, Speech speech)
    {
        if (name != Name)
            External.Add(speech);
    }

    private IEnumerator DetectLoop()
    {
        while (this.enabled)
        {
            if (ObjectDetector.VisibleObjects.Count == 0)
            {
                Visual = "No objects are visible.";

                yield return new WaitForSeconds(delay);
                continue;
            }

            Visual = $"The following objects ({ObjectDetector.VisibleObjects.Count}) are visible:\n";
            int counter = 0;
            foreach (ObjectDetector.DetectedObject obj in ObjectDetector.VisibleObjects.Values)
            {
                Visual += $"- {obj.DetectableObject.GetShortDescription()}\n";
                counter++;
            }

            ExternalText = "";

            for (int i = External.Count - 1; i >= 0; i--)
            {
                if ((DateTime.Now - External[i].CreatedTime).TotalSeconds < speechMemoryTime)
                {
                    ExternalText += $"- {External[i].Name}: {External[i].Text}\n";
                }
                else
                {
                    External.RemoveAt(i);
                }
            }

            yield return new WaitForSeconds(delay);
        }
    }

    private void SayAloud(string speech)
    {
        AloudSpeech?.Invoke(Name, new Speech(speech, Name));
        SpeechUI.text = speech;
    }

    private IEnumerator StartSession()
    {
        yield return new WaitForSeconds(delay + 0.5f);
        // Создание сессии
        var requestData = new StartSessionRequest
        {
            provider_type = "InstructLLM",
            person_id = "1",
            target_tickrate = 20
        };

        var jsonData = JsonUtility.ToJson(requestData);

        //Debug.Log(jsonData);
        using (UnityWebRequest www = UnityWebRequest.PostWwwForm($"{Domain}/start_session", "application/json"))
        {
            www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error starting session: " + www.error);
            }
            else
            {
                var jsonResponse = JsonUtility.FromJson<StartSessionResponse>(www.downloadHandler.text);
                if (jsonResponse.session_id != null)
                {
                    sessionId = jsonResponse.session_id;
                    isSessionActive = true;
                    StartCoroutine(ServerLoop());
                }
                else
                {
                    Debug.LogError("Session ID not found in response.");
                }
            }
        }
    }

    private void StopSession()
    {
        // Остановка сессии
        var requestData = new StopSessionRequest
        {
            session_id = sessionId
        };

        var jsonData = JsonUtility.ToJson(requestData);


        using (UnityWebRequest www = UnityWebRequest.Delete($"{Domain}/stop_session"))
        {
            www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            www.SetRequestHeader("Content-Type", "application/json");
            www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error stopping session: " + www.error);
            }
            else
            {
                Debug.Log("Session stopped successfully.");
            }
        }
    }

    private IEnumerator ServerLoop()
    {
        while (this.enabled)
        {
            // Установка контекста
            yield return StartCoroutine(SetState());

            // Получение ответа
            yield return StartCoroutine(GetResponse());

            yield return new WaitForSeconds(UpdateDelay);
        }
    }

    private IEnumerator SetState()
    {
        var requestData = new UpdateStateRequest
        {
            session_id = sessionId,
            visual = Visual,
            external = ExternalText,
            episodic = Episodic
        };

        var jsonData = JsonUtility.ToJson(requestData);

        using (UnityWebRequest www = UnityWebRequest.Put($"{Domain}/set_state", "application/json"))
        {
            www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error updating state: " + www.error);
            }
            else
            {
                //Debug.Log("State updated successfully.");
            }
        }
    }

    private IEnumerator GetResponse()
    {
        using (UnityWebRequest www = UnityWebRequest.Get($"{Domain}/get_response?session_id=" + sessionId))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error getting response: " + www.error);
            }
            else
            {
                var jsonResponse = JsonUtility.FromJson<GetResponseData>(www.downloadHandler.text);

                Debug.Log(www.downloadHandler.text);

                Debug.Log(jsonResponse.response.Count);

                if (jsonResponse.response.Count > 0)
                {
                    SpeechUI.text = "";

                    foreach (var speech in jsonResponse.response)
                        SayAloud(speech);
                }

                CurrentState = jsonResponse.state;
            }
        }
    }
}
