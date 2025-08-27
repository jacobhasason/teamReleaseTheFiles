using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.FPS.UI;

public class MenuNavigation : MonoBehaviour
{
    // Data to be passed in
    public static int AudienceCurrency;
    public static int CorporateCurrency;
    public static int waveCount;

    [HideInInspector]
    public WaveSpawner waveSpawner; // assign from inspector or find dynamically

    void Start()
    {
        if (waveSpawner == null)
            waveSpawner = FindObjectOfType<WaveSpawner>(); // ensures reference is set
        if (waveSpawner == null)
            Debug.Log("No wave spawner found!");

            Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("Menu started");
    }

    public void OnOptionSelected(int index)
    {
        Debug.Log("Option " + index + " selected!");
        waveSpawner?.OnRewardSelected(); // call back to WaveSpawner
    }

    public void OnSkip()
    {
        Debug.Log("Skipped reward!");
        waveSpawner?.OnRewardSelected(); // call back to WaveSpawner
    }
}
