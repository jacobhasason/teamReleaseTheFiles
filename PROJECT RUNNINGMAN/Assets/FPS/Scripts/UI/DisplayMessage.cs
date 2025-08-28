using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DisplayMessage : MonoBehaviour
{
    [Tooltip("The text that will be displayed")]
    [TextArea] public string message;

    [Tooltip("Prefab for the message (should have a Text or TMP component)")]
    public GameObject messagePrefab;

    [Tooltip("Parent Transform for the message (usually your HUD Canvas)")]
    public Transform parentTransform;

    [Tooltip("Delay before showing the message")]
    public float delayBeforeShowing = 0f;

    [Tooltip("How long the message stays on screen")]
    public float duration = 2f;

    [Tooltip("Fade out speed for message text")]
    public float fadeSpeed = 2f;

    public bool m_WasDisplayed = false;
    public float m_InitTime;

    void Start()
{
    if (messagePrefab == null)
    {
        Debug.LogWarning("Message prefab not assigned! Attempting to find in Resources...");
        messagePrefab = Resources.Load<GameObject>("MessagePrefab");
    }

    if (parentTransform == null)
    {
        Debug.LogWarning("Parent Transform not assigned! Using HUD canvas as fallback...");
        parentTransform = FindObjectOfType<Canvas>().transform;
    }

    m_InitTime = Time.time;
    m_WasDisplayed = false;
}


   /* void Update()
    {
        if (m_WasDisplayed)
            return;

        if (Time.time - m_InitTime > delayBeforeShowing)
        {
            GameObject messageInstance = Instantiate(messagePrefab, parentTransform);
            Text textComp = messageInstance.GetComponent<Text>();

            if (textComp != null)
                textComp.text = message;

            // Start coroutine to fade & destroy
            StartCoroutine(FadeAndDestroy(messageInstance));

            m_WasDisplayed = true;
        }
    }*/

    public void ShowMessage(string text, float duration)
    {
        message = text;
        delayBeforeShowing = 0f;
        this.duration = duration;
        m_WasDisplayed = false; // reset so it can trigger again
    }

    IEnumerator FadeAndDestroy(GameObject instance)
    {
        yield return new WaitForSeconds(duration);

        Text textComp = instance.GetComponent<Text>();
        if (textComp != null)
        {
            Color originalColor = textComp.color;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * fadeSpeed;
                textComp.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f - t);
                yield return null;
            }
        }

        Destroy(instance);
    }
}
