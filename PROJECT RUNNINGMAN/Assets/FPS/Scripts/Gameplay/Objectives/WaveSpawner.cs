using UnityEngine;
using Unity.FPS.Game;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Unity.FPS.Gameplay
{
    public class WaveSpawner : MonoBehaviour
    {
        public GameObject EnemyPrefab;
        public Transform[] SpawnPoints;
        public int MaxEnemies = 20;
        public float SpawnDelay = 2f;
        public float TimeBetweenWaves = 3f;

        private int enemiesSpawned = 0;
        private int enemiesAlive = 0;
        private int waveCount = 0;
        private bool waitingForNextWave = false;

        ObjectiveKillEnemies killObjective;

        void Start()
        {

            killObjective = FindObjectOfType<ObjectiveKillEnemies>();
            StartCoroutine(StartNextWaveWithDelay());
        }

        void RegisterEnemy(GameObject enemy)
        {
            var health = enemy.GetComponent<Health>();

            // Defensive: avoid double-calling on death
            bool hasDied = false;

            enemiesAlive++;
            Debug.Log($"[WaveSpawner] Enemy registered. enemiesAlive = {enemiesAlive}");

            health.OnDie += () =>
            {
                if (hasDied) return;
                hasDied = true;

                enemiesAlive--;

                EventManager.Broadcast(new EnemyKillEvent());
                OnEnemyKilled();
            };
        }


        public void OnEnemyKilled()
        {
            
            Debug.Log($"Enemy killed. Remaining alive: {enemiesAlive}");

            // Check after a slight delay to allow deaths to register cleanly
            if (enemiesAlive <= 0 && !waitingForNextWave && enemiesSpawned < MaxEnemies)
            {
                waitingForNextWave = true;
                StartCoroutine(StartNextWaveWithDelay());
            }
        }

        IEnumerator StartNextWaveWithDelay()
        {
            yield return new WaitForSeconds(TimeBetweenWaves);
            SpawnNextWave();
        }

        void SpawnNextWave()
        {
            waveCount++;
            waitingForNextWave = false;

            enemiesSpawned = waveCount;

            Debug.Log($"Spawning Wave {waveCount} with {enemiesSpawned} enemies");

            for (int i = 0; i < enemiesSpawned; i++)
            {
                Transform spawnPoint = SpawnPoints[Random.Range(0, SpawnPoints.Length)];
                GameObject enemy = Instantiate(EnemyPrefab, spawnPoint.position, spawnPoint.rotation);
                RegisterEnemy(enemy);
            }

            // Broadcast wave message
            DisplayMessageEvent waveMessage = Events.DisplayMessageEvent;
            waveMessage.Message = $"Wave {waveCount} Started!";
            waveMessage.DelayBeforeDisplay = 0f;
            EventManager.Broadcast(waveMessage);

            if (killObjective != null)
            {
                killObjective.SetRemainingEnemyCount(enemiesAlive);
            }
        }
    }
}
