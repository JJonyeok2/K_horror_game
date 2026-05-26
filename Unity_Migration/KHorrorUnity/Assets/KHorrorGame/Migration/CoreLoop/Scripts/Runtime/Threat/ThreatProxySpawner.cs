using UnityEngine;

namespace KHorrorGame.Migration
{
    public sealed class ThreatProxySpawner : MonoBehaviour
    {
        [SerializeField] private GameLoopController gameLoop;
        [SerializeField] private GameObject ghostProxy;
        [SerializeField] private GameObject dokkaebiProxy;
        [SerializeField] private Light spawnCueLight;
        [SerializeField] private int ghostRevealStage = 2;
        [SerializeField] private int dokkaebiRevealStage = 1;

        public bool GhostSpawned { get; private set; }
        public bool DokkaebiSpawned { get; private set; }

        private void Awake()
        {
            if (gameLoop == null)
            {
                gameLoop = FindObjectOfType<GameLoopController>();
            }

            SetProxyVisible(ghostProxy, false);
            SetProxyVisible(dokkaebiProxy, false);
            SetCueVisible(false);
        }

        private void Update()
        {
            if (gameLoop == null || gameLoop.State == null || gameLoop.ThreatGate == null)
            {
                return;
            }

            EvaluateThreats(
                gameLoop.ThreatGate.CanSpawnThreats,
                gameLoop.Resentment.Stage(),
                gameLoop.State.CurrentMap);
        }

        public void EvaluateThreats(bool canSpawnThreats, int resentmentStage, GameMapId currentMap)
        {
            if (!canSpawnThreats || currentMap != GameMapId.JonggaEstate)
            {
                return;
            }

            if (!DokkaebiSpawned && resentmentStage >= dokkaebiRevealStage)
            {
                DokkaebiSpawned = true;
                SetProxyVisible(dokkaebiProxy, true);
                SetCueVisible(true);
            }

            if (!GhostSpawned && resentmentStage >= ghostRevealStage)
            {
                GhostSpawned = true;
                SetProxyVisible(ghostProxy, true);
                SetCueVisible(true);
            }
        }

        private static void SetProxyVisible(GameObject proxy, bool visible)
        {
            if (proxy == null)
            {
                return;
            }

            foreach (var renderer in proxy.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = visible;
            }

            foreach (var collider in proxy.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }
        }

        private void SetCueVisible(bool visible)
        {
            if (spawnCueLight != null)
            {
                spawnCueLight.enabled = visible;
            }
        }
    }
}
