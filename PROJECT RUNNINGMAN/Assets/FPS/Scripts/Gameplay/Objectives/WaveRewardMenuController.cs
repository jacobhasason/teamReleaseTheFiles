using UnityEngine;
using UnityEngine.UI;
using System;
using Unity.FPS.Game;

public class WaveRewardMenuController : MonoBehaviour
{
    public Button[] optionButtons;
    public Button skipButton;
    public bool MenuClosed { get; private set; }

    public WaveSpawner waveSpawner;

    void Start()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Mouse click detected!");
        }
    }


    public void SetupMenu()
    {
        Debug.Log("Menu Open!");

        gameObject.SetActive(true);
        MenuClosed = false;

        // Make sure Unity processes the SetActive before wiring events
        StartCoroutine(SetupButtonsNextFrame());

        // Show cursor so player can click buttons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Menu Set!");
    }

    private System.Collections.IEnumerator SetupButtonsNextFrame()
    {
        yield return null; // wait 1 frame so UI is active

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() =>
            {
                Debug.Log($"Reward Attempted! {index}");
                OnRewardSelected(index);
            });

            if (optionButtons[i] == null)
            {
                Debug.LogError($"Button {i} is NULL in optionButtons!");
            }
            else
            {
                Debug.Log($"Button {i} is hooked: {optionButtons[i].name}");
            }

        }

        skipButton.onClick.RemoveAllListeners();
        skipButton.onClick.AddListener(() =>
        {
            Debug.Log("Skip Attempted!");
            OnRewardSkipped();
        });

        if (skipButton == null)
        {
            Debug.LogError("Skip button is NULL!");
        }
        else
        {
            Debug.Log($"Skip button is hooked: {skipButton.name}");
        }

        Debug.Log("Listeners hooked up!");
    }

    public void OnRewardSelected(int optionIndex)
    {
        Debug.Log("Reward Selected!");
        waveSpawner.SpawnNextWave();
        CloseMenu();
    }

    public void OnRewardSkipped()
    {
        Debug.Log("Reward Skipped!");
        waveSpawner.SpawnNextWave();
        CloseMenu();
    }

    public void CloseMenu()
    {
        MenuClosed = true;
        gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Closing Menu");
    }
}
