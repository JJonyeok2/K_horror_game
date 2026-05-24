using UnityEngine;

namespace KHorrorGame.Migration
{
    public sealed class RuntimeThreatSpawner : MonoBehaviour
    {
        [SerializeField] private GameLoopController gameLoop;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private EnemyBrain ghostActor;
        [SerializeField] private EnemyBrain dokkaebiActor;
        [SerializeField] private Transform ghostSpawnAnchor;
        [SerializeField] private Transform dokkaebiSpawnAnchor;
        [SerializeField] private Light spawnCueLight;
        [SerializeField] private float evaluationIntervalSeconds = 0.45f;
        [SerializeField] private float gatePlaneZ = 54f;

        private readonly ThreatDirector director = new ThreatDirector();
        private float evaluationTimer;

        private void Awake()
        {
            EnsureReferences();
            HideActor(ghostActor);
            HideActor(dokkaebiActor);
            SetCueVisible(false);
        }

        private void Update()
        {
            EnsureReferences();
            var targetTerritory = ResolvePlayerTerritory();

            evaluationTimer -= Time.deltaTime;
            if (evaluationTimer <= 0f)
            {
                EvaluateCurrent(targetTerritory);
                evaluationTimer = Mathf.Max(0.05f, evaluationIntervalSeconds);
            }

            TickActor(ghostActor, targetTerritory);
            TickActor(dokkaebiActor, targetTerritory);
        }

        public ThreatDirectorDecision EvaluateThreats(
            bool canSpawnThreats,
            int resentmentStage,
            GameMapId currentMap,
            TerritoryKind playerTerritory)
        {
            var decision = director.Evaluate(new ThreatDirectorContext(
                currentMap,
                playerTerritory,
                resentmentStage,
                canSpawnThreats,
                IsActorActive(ghostActor) ? 1 : 0,
                IsActorActive(dokkaebiActor) ? 1 : 0,
                resentmentStage * 31));

            ApplyDecision(decision);
            EnsureInteriorGhostAtMaximumThreat(canSpawnThreats, resentmentStage, currentMap);
            return decision;
        }

        private void EvaluateCurrent(TerritoryKind targetTerritory)
        {
            if (gameLoop == null || gameLoop.State == null || gameLoop.ThreatGate == null)
            {
                return;
            }

            EvaluateThreats(
                gameLoop.ThreatGate.CanSpawnThreats,
                gameLoop.Resentment.Stage(),
                gameLoop.State.CurrentMap,
                targetTerritory);
        }

        private void ApplyDecision(ThreatDirectorDecision decision)
        {
            if (decision == null)
            {
                SetCueVisible(false);
                return;
            }

            if (decision.Action == ThreatDirectorAction.SpawnGhost)
            {
                ActivateActor(ghostActor, ghostSpawnAnchor, EnemyKind.Ghost, TerritoryKind.EstateInterior, decision.Profile);
                SetCueVisible(true);
                return;
            }

            if (decision.Action == ThreatDirectorAction.SpawnDokkaebi)
            {
                ActivateActor(dokkaebiActor, dokkaebiSpawnAnchor, EnemyKind.Dokkaebi, TerritoryKind.ForestApproach, decision.Profile);
                SetCueVisible(true);
                return;
            }

            SetCueVisible(decision.Action == ThreatDirectorAction.CueOnly);
        }

        private void ActivateActor(
            EnemyBrain actor,
            Transform spawnAnchor,
            EnemyKind enemyKind,
            TerritoryKind homeTerritory,
            ThreatStageProfile profile)
        {
            if (actor == null || spawnAnchor == null || playerTarget == null)
            {
                return;
            }

            actor.transform.SetPositionAndRotation(spawnAnchor.position, spawnAnchor.rotation);
            actor.gameObject.SetActive(true);
            actor.SetAutomaticTick(false);
            actor.Configure(enemyKind, profile, playerTarget, homeTerritory, spawnAnchor.position);
        }

        private void EnsureInteriorGhostAtMaximumThreat(bool canSpawnThreats, int resentmentStage, GameMapId currentMap)
        {
            if (!canSpawnThreats
                || currentMap != GameMapId.JonggaEstate
                || resentmentStage < ThreatStageProfile.MaxStage
                || IsActorActive(ghostActor))
            {
                return;
            }

            ActivateActor(
                ghostActor,
                ghostSpawnAnchor,
                EnemyKind.Ghost,
                TerritoryKind.EstateInterior,
                ThreatStageProfile.ForStage(resentmentStage));
        }

        private void TickActor(EnemyBrain actor, TerritoryKind targetTerritory)
        {
            if (!IsActorActive(actor))
            {
                return;
            }

            actor.ManualTick(Time.deltaTime, targetTerritory);
        }

        private TerritoryKind ResolvePlayerTerritory()
        {
            if (gameLoop == null || gameLoop.State == null)
            {
                return TerritoryKind.BongoHub;
            }

            if (gameLoop.State.CurrentMap != GameMapId.JonggaEstate)
            {
                return TerritoryKind.BongoHub;
            }

            if (playerTarget != null && playerTarget.position.z < gatePlaneZ)
            {
                return TerritoryKind.ForestApproach;
            }

            return TerritoryKind.EstateInterior;
        }

        private void EnsureReferences()
        {
            if (gameLoop == null)
            {
                gameLoop = FindObjectOfType<GameLoopController>();
            }

            if (playerTarget == null)
            {
                var player = FindObjectOfType<UnityPlayerController>();
                if (player != null)
                {
                    playerTarget = player.transform;
                }
            }
        }

        private static bool IsActorActive(EnemyBrain actor)
        {
            return actor != null && actor.gameObject.activeSelf;
        }

        private static void HideActor(EnemyBrain actor)
        {
            if (actor != null)
            {
                actor.gameObject.SetActive(false);
                actor.SetAutomaticTick(false);
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
