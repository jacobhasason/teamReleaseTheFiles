using System.Collections;
using UnityEngine;
using Unity.FPS.Game;
using UnityEngine.UI;
using Unity.FPS.Gameplay;
using Unity.FPS.UI;


using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class WaveSpawner : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject[] EnemyPrefab;
    public Transform[] SpawnPoints;

    [Header("Wave Settings")]
    public int MaxEnemiesPerWave = 30;
    public float TimeBetweenWaves = 5f;

    [Header("Player")]
    public MonoBehaviour playerController; // drag  player movement script here

    [Header("UI & Feedback")]
    public DisplayMessage waveMessageManager;

    
    public int waveCount = 54;
    [HideInInspector]
    private int enemiesAlive = 0;
    private bool isWaitingBetweenWaves = false;
    [HideInInspector]
    public int enemiesKilled = 0;
  

    public PlayerInputHandler playerInputHandler;
    public EventSystem gameplayEventSystem; // assign your main EventSystem here
    private UIManager mainUIManager;
    public InGameMenuManager inGameMenuManager;
    //public MenuNavigation menuNavigation;

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
            StartCoroutine(CompleteWave());
        }

        enemiesKilled++;
    }

    // Called when a wave is finished
    public async Task OnWaveCompleted()
    {
        Debug.Log("Wave completed! Loading reward menu...");
        InGameMenuManager.BlockInput = true;
        Time.timeScale = 0f;
        
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

        InGameMenuManager.BlockInput = false;


        // Resume gameplay
        Time.timeScale = 1f;
        if (playerInputHandler != null)
        {
            playerInputHandler.allowInput = true;
            Debug.Log("Player Movement Enabled");
        }

        // Re-enable main EventSystem
        if (gameplayEventSystem != null)
        {
            gameplayEventSystem.gameObject.SetActive(true);
            Debug.Log("Event System Enabled");

        }

        SpawnNextWave();
    }

    public void SpawnNextWave()
    {
        waveCount++;
        StartCoroutine(SpawnWaveCoroutine());
    }

    private IEnumerator SpawnWaveCoroutine()
    {
        // Broadcast wave message
        DisplayMessageEvent waveMessage = Events.DisplayMessageEvent;
        waveMessage.Message = $"Wave {waveCount} Started!";
        waveMessage.DelayBeforeDisplay = 0f;
        EventManager.Broadcast(waveMessage);

        yield return new WaitForSeconds(1.5f);

        // Spawn enemies one by one
        int enemiesToSpawn = Mathf.Min(waveCount, MaxEnemiesPerWave);
        enemiesAlive = enemiesToSpawn;

        Debug.Log($"Spawning Wave {waveCount} with {enemiesAlive} enemies.");

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            Transform spawnPoint = SpawnPoints[Random.Range(0, SpawnPoints.Length)];
            GameObject enemy = Instantiate(EnemyPrefab[Random.Range(0, EnemyPrefab.Length)], spawnPoint.position, spawnPoint.rotation);

            var health = enemy.GetComponent<Health>();
            var controller = enemy.GetComponent<EnemyAI>();

            if (health != null)
            {
                // Gradually Increase stats of enemies when max enemy count is reached
                if (waveCount > MaxEnemiesPerWave)
                {
                    float gradInc = (waveCount - MaxEnemiesPerWave) / 2;
                    controller.walkSpeed += gradInc;
                    controller.runSpeed += gradInc;
                    health.MaxHealth += 5;
                }

                bool hasDied = false;
                health.OnDie += () =>
                {
                    if (hasDied) return;
                    hasDied = true;
                    OnEnemyKilled();
                };
            }

            yield return new WaitForSeconds(0.5f);
        }
    }
    private IEnumerator CompleteWave()
    {
        Time.timeScale = .3f;
        yield return new WaitForSeconds(.5f);
        OnWaveCompleted();
    }


}
