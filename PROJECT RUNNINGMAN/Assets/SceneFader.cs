using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    public float fadeDuration = 0.6f;

    public CanvasGroup cg;
    static SceneFader instance;

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        cg = GetComponentInChildren<CanvasGroup>(includeInactive: true);
        if (!cg) Debug.LogError("CanvasGroup not found on SceneFader.");
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
    }

    public void FadeToScene(string sceneName)
    {
        StartCoroutine(FadeSequence(sceneName));
    }

    IEnumerator FadeSequence(string sceneName)
    {
        // Fade out
        yield return StartCoroutine(Fade(1f));

        // Load next scene (async)
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (!op.isDone) yield return null;

        // Optional: wait one frame to let scene settle
        yield return null;

        // Fade in
        yield return StartCoroutine(Fade(0f));
    }

    IEnumerator Fade(float target)
    {
        cg.blocksRaycasts = true;
        float start = cg.alpha;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        cg.alpha = target;
        cg.blocksRaycasts = target > 0.001f; // only block while black
    }
}
