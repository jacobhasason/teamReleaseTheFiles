using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;

namespace Unity.FPS.UI
{
    public class LoadoutManager : MonoBehaviour
    {
        public GameObject PreviewText;

        private int upcomingStageCount = 0;
        private int upcomingEnemyCount = 0;
        private int enemiesTotal = 0;

        private int waveCount = 0;
        private bool waitingForNextStage = false;

        private StageComp nextStage;
        

        void Start()
        {   
            nextStage = RunManager.Instance.CurrentRunMap.RunStages[RunManager.Instance.RunLevel];
            upcomingStageCount = RunManager.Instance.RunLevel;
            enemiesTotal = 0;
            for (int i = 0; i < nextStage.EnemyWaves.Length; i++)
            {
                enemiesTotal += nextStage.EnemyWaves[i].EnemyCount;
            }
            
            if (PreviewText)
            {
                
            }



        }
    }
}
