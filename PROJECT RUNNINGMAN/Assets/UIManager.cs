using UnityEngine;

public class UIManager : MonoBehaviour
{
    private Canvas[] mainSceneCanvases;

    public void DisableMainUI()
    {
        // Find all Canvas objects in the scene
        mainSceneCanvases = FindObjectsOfType<Canvas>(true);

        // Disable them so they don’t interfere with the additive menu
        foreach (var c in mainSceneCanvases)
        {
            c.gameObject.SetActive(false);
        }
    }

    public void EnableMainUI()
    {
        foreach (var c in mainSceneCanvases)
        {
            c.gameObject.SetActive(true);
        }
    }
}
