using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    public class WaveComp : MonoBehaviour
    {
        [Header("WaveDetails")]
        [Tooltip("the objectives mandatory for completion of the wave")]
        public Objective[] NeededObjectives;
        [Tooltip("the number of enemies spawned within the wave")]
        public int EnemyCount;
        [Tooltip("the prefab for the enemy spawned within the wave")]
        public GameObject EnemyPrefab;
        [Tooltip("the transforms from which enemies will spawn")]
        public Transform[] EnemySpawns;
    }
}
