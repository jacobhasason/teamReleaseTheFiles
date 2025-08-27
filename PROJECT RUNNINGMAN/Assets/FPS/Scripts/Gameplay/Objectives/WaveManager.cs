using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.FPS.Game;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    public class WaveManager : MonoBehaviour
    {
        public GameObject EnemyPrefab;
        public Transform[] SpawnPoints;
        public int MaxEnemies = 20;
        public float SpawnDelay = 2f;
        public float TimeBetweenWaves = 3f;

        private int enemiesSpawned = 0;
        private int enemiesAlive = 0;
        private int enemiesTotal = 0;
        private int waveCount = 0;
        private bool waitingForNextWave = false;
        private StageComp CurrentStage;
        private WaveComp CurrentWave;




        ObjectiveKillEnemies killObjective;

        void Start()
        {
            CurrentStage = RunManager.Instance.CurrentRunMap.RunStages[RunManager.Instance.RunLevel];
            killObjective = FindObjectOfType<ObjectiveKillEnemies>();
            if (killObjective)
            {
                enemiesTotal = 0;
                for (int i = 0; i < CurrentStage.EnemyWaves.Length; i ++)
                {
                    enemiesTotal += CurrentStage.EnemyWaves[i].EnemyCount;
                }
                MaxEnemies = enemiesTotal;
            }
            RunManager.Instance.RunLevel++;
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
            CurrentWave = CurrentStage.EnemyWaves[waveCount];
            enemiesSpawned = CurrentWave.EnemyCount;
            waveCount++;
            waitingForNextWave = false;

            

            Debug.Log($"Spawning Wave {waveCount} with {enemiesSpawned} enemies");

            for (int i = 0; i < enemiesSpawned; i++)
            {
                Transform spawnPoint = SpawnPoints[Random.Range(0, SpawnPoints.Length)];
                GameObject enemy = Instantiate(EnemyPrefab, spawnPoint.position, spawnPoint.rotation);
                RegisterEnemy(enemy);
            }

            // Broadcast wave message
            DisplayMessageEvent waveMessage = Events.DisplayMessageEvent;
            waveMessage.Message = $"Wave {waveCount} of {RunManager.Instance.CurrentRunMap.RunStages[RunManager.Instance.RunLevel]} Started!";
            waveMessage.DelayBeforeDisplay = 0f;
            EventManager.Broadcast(waveMessage);

            DisplayMessageEvent enemyCountMessage = Events.DisplayMessageEvent;
            if (enemiesSpawned > 1)
            {
                waveMessage.Message = $"{enemiesAlive} warriors approaching!";
            }
            else
            {
                waveMessage.Message = $"{enemiesAlive} warrior approaching!";
            }
            waveMessage.DelayBeforeDisplay = 1.3f;
            EventManager.Broadcast(enemyCountMessage);

            if (killObjective != null)
            {
                killObjective.SetRemainingEnemyCount(enemiesAlive);
            }
        }
    }
}
