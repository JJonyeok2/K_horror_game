using UnityEngine;

namespace KHorrorGame.Migration
{
    public sealed class RuntimeThreatSpawner : MonoBehaviour
    {
        [SerializeField] private GameLoopController gameLoop;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private EnemyBrain ghostActor;
        [SerializeField] private EnemyBrain dokkaebiActor;
        [SerializeField] private EnemyBrain[] ghostActors;
        [SerializeField] private EnemyBrain[] dokkaebiActors;
        [SerializeField] private Transform ghostSpawnAnchor;
        [SerializeField] private Transform dokkaebiSpawnAnchor;
        [SerializeField] private Transform[] ghostSpawnAnchors;
        [SerializeField] private Transform[] dokkaebiSpawnAnchors;
        [SerializeField] private Light spawnCueLight;
        [SerializeField] private float evaluationIntervalSeconds = 0.45f;
        [SerializeField] private float gatePlaneZ = 54f;

        private readonly ThreatDirector director = new ThreatDirector();
        private float evaluationTimer;

        private void Awake()
        {
            EnsureReferences();
            EnsureGhostControllers(GetGhostActors());
            HideActors(GetGhostActors());
            HideActors(GetDokkaebiActors());
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

            TickActors(GetGhostActors(), targetTerritory);
            TickActors(GetDokkaebiActors(), targetTerritory);
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
                CountActive(GetGhostActors()),
                CountActive(GetDokkaebiActors()),
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
                ActivateNextActor(
                    GetGhostActors(),
                    GetGhostSpawnAnchors(),
                    ghostSpawnAnchor,
                    EnemyKind.Ghost,
                    TerritoryKind.EstateInterior,
                    decision.Profile);
                SetCueVisible(true);
                return;
            }

            if (decision.Action == ThreatDirectorAction.SpawnDokkaebi)
            {
                ActivateNextActor(
                    GetDokkaebiActors(),
                    GetDokkaebiSpawnAnchors(),
                    dokkaebiSpawnAnchor,
                    EnemyKind.Dokkaebi,
                    TerritoryKind.ForestApproach,
                    decision.Profile);
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

            var controller = EnsureControllerFor(actor, enemyKind);
            actor.transform.SetPositionAndRotation(spawnAnchor.position, spawnAnchor.rotation);
            actor.gameObject.SetActive(true);
            actor.Configure(enemyKind, profile, playerTarget, homeTerritory, spawnAnchor.position);
            actor.SetAutomaticTick(false);
            if (controller != null)
            {
                controller.Configure(actor, playerTarget, homeTerritory, spawnAnchor.position);
                controller.SetAutomaticTick(false);
            }
        }

        private void EnsureInteriorGhostAtMaximumThreat(bool canSpawnThreats, int resentmentStage, GameMapId currentMap)
        {
            if (!canSpawnThreats
                || currentMap != GameMapId.JonggaEstate
                || resentmentStage < ThreatStageProfile.MaxStage
                || CountActive(GetGhostActors()) > 0)
            {
                return;
            }

            ActivateNextActor(
                GetGhostActors(),
                GetGhostSpawnAnchors(),
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

            var controller = actor.GetComponent<EnemyController>();
            if (controller != null)
            {
                controller.ManualTick(Time.deltaTime, targetTerritory);
                return;
            }

            actor.ManualTick(Time.deltaTime, targetTerritory);
        }

        private void TickActors(EnemyBrain[] actors, TerritoryKind targetTerritory)
        {
            foreach (var actor in actors)
            {
                TickActor(actor, targetTerritory);
            }
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

        private EnemyBrain[] GetGhostActors()
        {
            return ResolveActors(ghostActors, ghostActor);
        }

        private EnemyBrain[] GetDokkaebiActors()
        {
            return ResolveActors(dokkaebiActors, dokkaebiActor);
        }

        private Transform[] GetGhostSpawnAnchors()
        {
            return ResolveAnchors(ghostSpawnAnchors, ghostSpawnAnchor);
        }

        private Transform[] GetDokkaebiSpawnAnchors()
        {
            return ResolveAnchors(dokkaebiSpawnAnchors, dokkaebiSpawnAnchor);
        }

        private static EnemyBrain[] ResolveActors(EnemyBrain[] pool, EnemyBrain fallback)
        {
            if (pool != null && pool.Length > 0)
            {
                return pool;
            }

            return fallback != null ? new[] { fallback } : new EnemyBrain[0];
        }

        private static Transform[] ResolveAnchors(Transform[] pool, Transform fallback)
        {
            if (pool != null && pool.Length > 0)
            {
                return pool;
            }

            return fallback != null ? new[] { fallback } : new Transform[0];
        }

        private static int CountActive(EnemyBrain[] actors)
        {
            var active = 0;
            foreach (var actor in actors)
            {
                if (IsActorActive(actor))
                {
                    active++;
                }
            }

            return active;
        }

        private static void HideActor(EnemyBrain actor)
        {
            if (actor != null)
            {
                actor.gameObject.SetActive(false);
                actor.SetAutomaticTick(false);
                var controller = actor.GetComponent<EnemyController>();
                if (controller != null)
                {
                    controller.SetAutomaticTick(false);
                }
            }
        }

        private static void HideActors(EnemyBrain[] actors)
        {
            foreach (var actor in actors)
            {
                HideActor(actor);
            }
        }

        private void ActivateNextActor(
            EnemyBrain[] actors,
            Transform[] anchors,
            Transform fallbackAnchor,
            EnemyKind enemyKind,
            TerritoryKind homeTerritory,
            ThreatStageProfile profile)
        {
            for (var i = 0; i < actors.Length; i++)
            {
                var actor = actors[i];
                if (IsActorActive(actor))
                {
                    continue;
                }

                ActivateActor(actor, SelectAnchor(anchors, fallbackAnchor, i), enemyKind, homeTerritory, profile);
                return;
            }
        }

        private static Transform SelectAnchor(Transform[] anchors, Transform fallbackAnchor, int actorIndex)
        {
            if (anchors != null && anchors.Length > 0)
            {
                return anchors[Mathf.Clamp(actorIndex, 0, anchors.Length - 1)];
            }

            return fallbackAnchor;
        }

        private void SetCueVisible(bool visible)
        {
            if (spawnCueLight != null)
            {
                spawnCueLight.enabled = visible;
            }
        }

        private static void EnsureGhostControllers(EnemyBrain[] actors)
        {
            foreach (var actor in actors)
            {
                EnsureControllerFor(actor, EnemyKind.Ghost);
            }
        }

        private static EnemyController EnsureControllerFor(EnemyBrain actor, EnemyKind enemyKind)
        {
            if (actor == null)
            {
                return null;
            }

            var controller = actor.GetComponent<EnemyController>();
            if (controller == null && enemyKind == EnemyKind.Ghost)
            {
                controller = actor.gameObject.AddComponent<GhostEnemy>();
            }

            if (controller != null)
            {
                controller.SetAutomaticTick(false);
            }

            return controller;
        }
    }
}
