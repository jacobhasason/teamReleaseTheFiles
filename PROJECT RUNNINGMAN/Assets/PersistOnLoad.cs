using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistOnLoad : MonoBehaviour
{
    [Tooltip("Scene this object should persist into, and be destroyed after leaving.")]
    public string sceneToSurvive = "LoseScene";

    private bool hasSurvivedTargetScene = false;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnLoaded;
    }

    private void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == sceneToSurvive)
        {
            // We’ve entered the LoseScreen
            hasSurvivedTargetScene = true;
        }
        else if (hasSurvivedTargetScene)
        {
            // We’ve already been through the LoseScreen and now left it ? clear
            Destroy(gameObject);
        }
    }
}
