using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ViewDescriptor : MonoBehaviour
{
    [field: SerializeField]
    public ObjectDetector ObjectDetector { get; private set; }

    [field: SerializeField]
    [field: Multiline(30)]
    public string Description { get; private set; }

    [SerializeField]
    [Min(0f)]
    private float delay = 0.5f;

    private void Start()
    {
        StartCoroutine(MainLoop());
    }

    private IEnumerator MainLoop()
    {
        while (this.enabled)
        {
            if (ObjectDetector.VisibleObjects.Count == 0)
            {
                Description = "No objects are visible.";

                yield return new WaitForSeconds(delay);
                continue;
            }

            Description = $"The following objects ({ObjectDetector.VisibleObjects.Count}) are visible:\n";

            foreach (ObjectDetector.DetectedObject obj in ObjectDetector.VisibleObjects.Values)
            {
                Description += $"- {obj.DetectableObject.GetDescription()}\n";
            }

            yield return new WaitForSeconds(delay);
        }
    }
}
