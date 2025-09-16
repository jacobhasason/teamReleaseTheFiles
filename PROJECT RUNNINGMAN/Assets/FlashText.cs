using System.Collections;
using UnityEngine;

public class FlashText : MonoBehaviour
{
    [Tooltip("The GameObject to flash (e.g. a TextMeshProUGUI object or UI text)")]
    public GameObject targetObject;

    [Tooltip("Time in seconds between flashes")]
    public float flashInterval = 2f;

    private Coroutine flashRoutine;

    void OnEnable()
    {
        // Start flashing when script becomes active
        flashRoutine = StartCoroutine(FlashCoroutine());
    }

    void OnDisable()
    {
        // Stop flashing when script is disabled
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);
    }

    IEnumerator FlashCoroutine()
    {
        while (true)
        {
            if (targetObject != null)
            {
                targetObject.SetActive(!targetObject.activeSelf);
            } else
            {
                Debug.Log("Text field not found!");
            }
            yield return new WaitForSeconds(flashInterval);
        }
    }
}
