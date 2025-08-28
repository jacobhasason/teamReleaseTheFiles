using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    public class RunMap : MonoBehaviour
    {
        [Header("Sequence")]
        [Tooltip("the sequence of stages the player will go through in a full run")]
        public StageComp[] RunStages;
    }
}
