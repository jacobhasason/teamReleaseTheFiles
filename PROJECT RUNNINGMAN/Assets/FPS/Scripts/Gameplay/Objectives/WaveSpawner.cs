using System.Collections;
using UnityEngine;
using Unity.FPS.Game;
using UnityEngine.UI;
using Unity.FPS.Gameplay;

using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class WaveSpawner : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject EnemyPrefab;
    public Transform[] SpawnPoints;

    [Header("Wave Settings")]
    public int MaxEnemiesPerWave = 20;
    public float TimeBetweenWaves = 3f;

    [Header("Player")]
    public MonoBehaviour playerController; // drag  player movement script here

    [Header("UI & Feedback")]
    public DisplayMessageEvent waveMessageEvent;

    private int waveCount = 0;
    private int enemiesAlive = 0;
    private bool isWaitingBetweenWaves = false;
  

    public PlayerInputHandler playerInputHandler;
    public EventSystem gameplayEventSystem; // assign your main EventSystem here
    public UIManager mainUIManager;


    void Start()
    {
        SpawnNextWave();
    }

    // Called whenever an enemy dies
    public void OnEnemyKilled()
    {
        enemiesAlive--;
        Debug.Log($"Enemy killed. Remaining alive: {enemiesAlive}");


        // If all enemies are killed display the wave reward menu
        if (enemiesAlive <= 0 && !isWaitingBetweenWaves)
        {
            OnWaveCompleted();
        }
    }

    // Called when a wave is finished
    public void OnWaveCompleted()
    {
        Debug.Log("Wave completed! Loading reward menu...");

        // Find reference back to the main scene’s manager
        mainUIManager = FindObjectOfType<UIManager>();

        // Disable the main scene UI while this menu is active
        if (mainUIManager != null)
        {
            mainUIManager.DisableMainUI();
        }

        // Freeze gameplay
        Time.timeScale = 0f;
        if (playerInputHandler != null)
        {
            playerInputHandler.allowInput = false;
        }

        // Disable main EventSystem
        if (gameplayEventSystem != null)
        {
            gameplayEventSystem.gameObject.SetActive(false);
        }
            
        // Load reward menu additively
        SceneManager.LoadScene("LoadoutMenu", LoadSceneMode.Additive);
    }

    public void OnRewardSelected()
    {
        Debug.Log("Reward selected, closing reward menu...");

        SceneManager.UnloadSceneAsync("LoadoutMenu");

        // Resume gameplay
        Time.timeScale = 1f;
        if (playerInputHandler != null)
            playerInputHandler.allowInput = true;

        // Re-enable main EventSystem
        if (gameplayEventSystem != null)
        {
            gameplayEventSystem.gameObject.SetActive(true);
        }

        // Re-enable the main scene UI
        if (mainUIManager != null)
            mainUIManager.EnableMainUI();

        SpawnNextWave();
    }

    public void SpawnNextWave()
    {
        waveCount++;
        if (waveMessageEvent != null)
        {
            waveMessageEvent.Message = $"Wave {waveCount} Completed!";
            waveMessageEvent.DelayBeforeDisplay = 0f;
            EventManager.Broadcast(waveMessageEvent);
        }

        int enemiesToSpawn = Mathf.Min(waveCount, MaxEnemiesPerWave);
        enemiesAlive = enemiesToSpawn;

        Debug.Log($"Spawning Wave {waveCount} with {enemiesAlive} enemies.");

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            Transform spawnPoint = SpawnPoints[Random.Range(0, SpawnPoints.Length)];
            GameObject enemy = Instantiate(EnemyPrefab, spawnPoint.position, spawnPoint.rotation);

            // Make sure each enemy notifies the manager when it dies
            var health = enemy.GetComponent<Health>();
            if (health != null)
            {
                bool hasDied = false;
                health.OnDie += () =>
                {
                    if (hasDied) return;
                    hasDied = true;
                    OnEnemyKilled();
                };
            }
        }

        // Optional: show UI message about wave starting
        if (waveMessageEvent != null)
        {
            waveMessageEvent.Message = $"Wave {waveCount} Started!";
            waveMessageEvent.DelayBeforeDisplay = 0f;
            EventManager.Broadcast(waveMessageEvent);
        }
    }
}
