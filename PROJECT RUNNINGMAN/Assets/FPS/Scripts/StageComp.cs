using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    public class StageComp : MonoBehaviour
    {
        [Header("StageDetails")]
        [Tooltip("the objectives mandatory for completion of the stage")]
        public Objective[] NeededObjectives;
        [Tooltip("the waves of enemies spawned within the stage")]
        public WaveComp[] EnemyWaves;
    }
}
