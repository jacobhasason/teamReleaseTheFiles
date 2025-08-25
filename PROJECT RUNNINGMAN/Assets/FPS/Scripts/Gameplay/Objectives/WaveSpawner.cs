using System.Collections;
using UnityEngine;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;

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
    public WaveRewardMenuController waveRewardMenu;

    public PlayerInputHandler playerInputHandler;

    void Start()
    {
        StartCoroutine(StartNextWaveWithDelay());
    }

    // Called whenever an enemy dies
    public void OnEnemyKilled()
    {
        enemiesAlive--;
        Debug.Log($"Enemy killed. Remaining alive: {enemiesAlive}");


        // If all enemies are killed display the wave reward menu
        if (enemiesAlive <= 0 && !isWaitingBetweenWaves)
        {
            StartCoroutine(PauseBetweenWaves());
        }
    }

    IEnumerator PauseBetweenWaves()
    {
        isWaitingBetweenWaves = true;

        if (playerInputHandler != null)
            playerInputHandler.allowInput = false;

        // Show the reward menu
        if (waveRewardMenu != null)
        {
            waveRewardMenu.SetupMenu();
            Debug.Log("Menu Opened!");
        }

        // Wait until the player closes the menu
        while (!waveRewardMenu.MenuClosed)
            yield return null;

        // Resume player movement
        if (playerInputHandler != null)
            playerInputHandler.allowInput = true;

        isWaitingBetweenWaves = false;

        SpawnNextWave();
    }


    IEnumerator StartNextWaveWithDelay()
    {
        yield return new WaitForSeconds(TimeBetweenWaves);
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
