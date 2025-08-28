using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    public class RunManager : MonoBehaviour
    {


        public static RunManager Instance;

        [Tooltip("the distance the player is through their run")]
        public int RunLevel = 0;
        [Tooltip("the distance of the run")]
        public int RunLevelTotal = 0;
        [Tooltip("the player's popularity with the dash's corporate sponsors")]
        public float SponsorPop = 25;
        [Tooltip("the player's popularity with the dash's wider audience")]
        public float AudiencePop = 25;

        [Tooltip("the shape of the player's run, described in stages and in waves")]
        public RunMap CurrentRunMap;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
