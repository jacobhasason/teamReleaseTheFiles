using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    public class SideO : MonoBehaviour
    {
        [Header("SideObjective info")]
        [Tooltip("the objectives mandatory for completion of the side objective")]
        public Objective[] NeededObjectives;
        [Tooltip("the prefab for the enemy spawned within a side objective")]
        public GameObject EnemyPrefab;
        [Tooltip("the reward given upon completion of the objective")]
        public Transform[] EnemySpawns;
        [Tooltip("the transforms from which location-based side objectives will be found")]
        public Transform[] SideOSpawns;
    }
}