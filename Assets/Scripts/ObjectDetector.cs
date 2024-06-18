using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectDetector : MonoBehaviour
{
    [Serializable]
    public class DetectedObject
    {
        public DetectedObject(DetectableObject detectableObject, float detectTime)
        {
            DetectableObject = detectableObject;
            DetectTime = detectTime;
        }

        [field: SerializeField]
        public DetectableObject DetectableObject { get; private set; }

        [field: SerializeField]
        public float DetectTime { get; private set; } = 0;

        public void UpdateDetectTime()
        {
            DetectTime = Time.realtimeSinceStartup;
        }
    }

    [SerializeField]
    private float viewRadius;

    [SerializeField]
    [Range(0, 180)]
    private float horizontalViewAngle = 180f;
    [SerializeField]
    [Range(0, 180)]
    private float verticalViewAngle = 180f;

    [SerializeField]
    [Min(5)]
    private int resolutionWidth = 640;
    [SerializeField]
    [Min(5)]
    private int resolutionHeight = 360;

    [SerializeField]
    private LayerMask targetMask;

    public Dictionary<string, DetectedObject> VisibleObjects { get; private set; } = new Dictionary<string, DetectedObject>();

    [SerializeField]
    [Min(0f)]
    private float visualMemoryTime = 20f;

    private void Update()
    {
        FilterVisibleObjects();
        FindVisibleTargets();
    }

    private void FilterVisibleObjects()
    {
        List<string> keysToRemove = new List<string>();

        foreach (var kvp in VisibleObjects)
        {
            DetectedObject obj = kvp.Value;
            if (Time.realtimeSinceStartup - obj.DetectTime > visualMemoryTime)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            VisibleObjects.Remove(key);
        }
    }

    private void FindVisibleTargets()
    {
        float horizontalAngleStep = horizontalViewAngle / (resolutionWidth - 1);
        float verticalAngleStep = verticalViewAngle / (resolutionHeight - 1);

        for (int y = 0; y < resolutionHeight; y++)
        {
            for (int x = 0; x < resolutionWidth; x++)
            {
                float angleH = -horizontalViewAngle / 2 + horizontalAngleStep * x;
                float angleV = -verticalViewAngle / 2 + verticalAngleStep * y;

                Quaternion localRotation = Quaternion.Euler(angleV, angleH, 0);
                Vector3 direction = transform.rotation * localRotation * Vector3.forward;

                RaycastHit hit;
                if (Physics.Raycast(transform.position, direction, out hit, viewRadius, targetMask))
                {
                    DetectableObject objectDescription = hit.transform.GetComponent<DetectableObject>();

                    if (objectDescription != null)
                        if (!VisibleObjects.ContainsKey(objectDescription.ID))
                        {
                            VisibleObjects.Add(objectDescription.ID, new DetectedObject(objectDescription, Time.realtimeSinceStartup));
                        } else
                        {
                            VisibleObjects[objectDescription.ID].UpdateDetectTime();
                        }
                }
            }
        }

#if UNITY_EDITOR
        if (showTextDebug)
            Debug.Log($"[{gameObject.name}] Visible targets: {VisibleObjects.Count}");
#endif
    }

#if UNITY_EDITOR

    [Header("Debug")]
    [SerializeField]
    private bool showTextDebug = false;
    [SerializeField]
    private bool showRayDebug = true;
    [SerializeField]
    private bool showEveryRay = false;

    void OnDrawGizmos()
    {
        if (!showRayDebug) return;

        Gizmos.color = Color.red;

        float horizontalAngleStep = horizontalViewAngle / (resolutionWidth - 1);
        float verticalAngleStep = verticalViewAngle / (resolutionHeight - 1);

        Vector3[] cornerPoints = new Vector3[4];

        for (int y = 0; y < resolutionHeight; y++)
        {
            for (int x = 0; x < resolutionWidth; x++)
            {
                float angleH = -horizontalViewAngle / 2 + horizontalAngleStep * x;
                float angleV = -verticalViewAngle / 2 + verticalAngleStep * y;

                Quaternion localRotation = Quaternion.Euler(angleV, angleH, 0);
                Vector3 direction = transform.rotation * localRotation * Vector3.forward;

                RaycastHit hit;
                float distance = viewRadius;
                if (Physics.Raycast(transform.position, direction, out hit, viewRadius, targetMask))
                {
                    distance = hit.distance;
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(transform.position + direction * distance, 0.03f);

                    Gizmos.DrawLine(transform.position, transform.position + direction * distance);

                }
                else if (showEveryRay)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(transform.position, transform.position + direction * viewRadius);
                }
            }
        }

        // Отрисовка синего контура конуса
        Gizmos.color = Color.white;
        DrawConeOutline(horizontalAngleStep, verticalAngleStep);
    }

    void DrawConeOutline(float horizontalAngleStep, float verticalAngleStep)
    {
        Vector3[] cornerPoints = new Vector3[4];
        int cornerIndex = 0;
        for (int i = 0; i <= resolutionWidth; i += resolutionWidth - 1)
        {
            for (int j = 0; j <= resolutionHeight; j += resolutionHeight - 1)
            {
                float angleH = -horizontalViewAngle / 2 + horizontalAngleStep * i;
                float angleV = -verticalViewAngle / 2 + verticalAngleStep * j;

                Quaternion localRotation = Quaternion.Euler(angleV, angleH, 0);
                Vector3 direction = transform.rotation * localRotation * Vector3.forward;
                Vector3 endPoint = transform.position + direction * viewRadius;

                Gizmos.DrawLine(transform.position, endPoint);
                cornerPoints[cornerIndex++] = endPoint;
            }
        }

        // Соединяем угловые точки
        Gizmos.DrawLine(cornerPoints[0], cornerPoints[1]);
        Gizmos.DrawLine(cornerPoints[0], cornerPoints[2]);
        Gizmos.DrawLine(cornerPoints[3], cornerPoints[1]);
        Gizmos.DrawLine(cornerPoints[3], cornerPoints[2]);
    }
#endif
}