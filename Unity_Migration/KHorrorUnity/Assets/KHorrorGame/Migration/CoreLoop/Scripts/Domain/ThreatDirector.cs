using System;

namespace KHorrorGame.Migration
{
    public enum ThreatDirectorAction
    {
        None,
        CueOnly,
        SpawnGhost,
        SpawnDokkaebi
    }

    public sealed class ThreatDirectorContext
    {
        public ThreatDirectorContext(
            GameMapId currentMap,
            TerritoryKind playerTerritory,
            int resentmentStage,
            bool canSpawnThreats,
            int activeGhostCount,
            int activeDokkaebiCount,
            int varianceSeed)
        {
            CurrentMap = currentMap;
            PlayerTerritory = playerTerritory;
            ResentmentStage = resentmentStage;
            CanSpawnThreats = canSpawnThreats;
            ActiveGhostCount = Math.Max(0, activeGhostCount);
            ActiveDokkaebiCount = Math.Max(0, activeDokkaebiCount);
            VarianceSeed = varianceSeed;
        }

        public GameMapId CurrentMap { get; }
        public TerritoryKind PlayerTerritory { get; }
        public int ResentmentStage { get; }
        public bool CanSpawnThreats { get; }
        public int ActiveGhostCount { get; }
        public int ActiveDokkaebiCount { get; }
        public int VarianceSeed { get; }
        public int ActiveThreatCount
        {
            get { return ActiveGhostCount + ActiveDokkaebiCount; }
        }
    }

    public sealed class ThreatDirectorDecision
    {
        public ThreatDirectorDecision(
            ThreatDirectorAction action,
            EnemyKind? enemyKind,
            ThreatStageProfile profile,
            string reason)
        {
            Action = action;
            EnemyKind = enemyKind;
            Profile = profile;
            Reason = reason;
        }

        public ThreatDirectorAction Action { get; }
        public EnemyKind? EnemyKind { get; }
        public ThreatStageProfile Profile { get; }
        public string Reason { get; }
    }

    public sealed class ThreatStageProfile
    {
        public const int MaxStage = 5;

        private static readonly string[] StateNames =
        {
            "dormant",
            "subtle_presence",
            "visible",
            "route_interference",
            "pursuit",
            "contested_extraction"
        };

        private static readonly int[] MaxActiveThreatsByStage = { 0, 1, 2, 3, 3, 4 };
        private static readonly int[] DamagePerHitByStage = { 0, 0, 0, 8, 20, 30 };
        private static readonly float[] AttackRangeByStage = { 0f, 0f, 0f, 1.35f, 2f, 2.4f };
        private static readonly float[] AttackIntervalSecondsByStage = { 0f, 0f, 0f, 2.8f, 2f, 1.35f };
        private static readonly float[] PursuitSpeedByStage = { 0f, 0f, 0f, 2.1f, 3.4f, 4.6f };
        private static readonly float[] PatternVarianceByStage = { 0f, 0.05f, 0.12f, 0.25f, 0.45f, 0.65f };

        private ThreatStageProfile(
            int stage,
            string stateName,
            int maxActiveThreats,
            int damagePerHit,
            float attackRange,
            float attackIntervalSeconds,
            float pursuitSpeed,
            float patternVariance)
        {
            Stage = stage;
            StateName = stateName;
            MaxActiveThreats = maxActiveThreats;
            DamagePerHit = damagePerHit;
            AttackRange = attackRange;
            AttackIntervalSeconds = attackIntervalSeconds;
            PursuitSpeed = pursuitSpeed;
            PatternVariance = patternVariance;
        }

        public int Stage { get; }
        public string StateName { get; }
        public int MaxActiveThreats { get; }
        public int DamagePerHit { get; }
        public float AttackRange { get; }
        public float AttackIntervalSeconds { get; }
        public float PursuitSpeed { get; }
        public float PatternVariance { get; }

        public static ThreatStageProfile ForStage(int stage)
        {
            var clampedStage = ClampStage(stage);
            return new ThreatStageProfile(
                clampedStage,
                StateNames[clampedStage],
                MaxActiveThreatsByStage[clampedStage],
                DamagePerHitByStage[clampedStage],
                AttackRangeByStage[clampedStage],
                AttackIntervalSecondsByStage[clampedStage],
                PursuitSpeedByStage[clampedStage],
                PatternVarianceByStage[clampedStage]);
        }

        private static int ClampStage(int stage)
        {
            if (stage < 0)
            {
                return 0;
            }

            if (stage > MaxStage)
            {
                return MaxStage;
            }

            return stage;
        }
    }

    public sealed class ThreatDirector
    {
        private readonly EnemyTerritoryRules territoryRules;

        public ThreatDirector()
            : this(EnemyTerritoryRules.Default)
        {
        }

        public ThreatDirector(EnemyTerritoryRules territoryRules)
        {
            this.territoryRules = territoryRules ?? EnemyTerritoryRules.Default;
        }

        public ThreatDirectorDecision Evaluate(ThreatDirectorContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var profile = ThreatStageProfile.ForStage(context.ResentmentStage);

            if (context.CurrentMap != GameMapId.JonggaEstate)
            {
                return None(profile, "not_estate");
            }

            if (profile.MaxActiveThreats <= 0)
            {
                return None(profile, "stage_dormant");
            }

            if (context.ActiveThreatCount >= profile.MaxActiveThreats)
            {
                return None(profile, "budget_full");
            }

            if (!context.CanSpawnThreats)
            {
                return Cue(profile, "grace_or_gate_blocked");
            }

            if (context.PlayerTerritory == TerritoryKind.ForestApproach)
            {
                return EvaluateForest(context, profile);
            }

            if (context.PlayerTerritory == TerritoryKind.EstateInterior)
            {
                return EvaluateEstateInterior(context, profile);
            }

            return None(profile, "invalid_territory");
        }

        private ThreatDirectorDecision EvaluateForest(
            ThreatDirectorContext context,
            ThreatStageProfile profile)
        {
            if (profile.Stage >= 3
                && context.ActiveDokkaebiCount < profile.MaxActiveThreats
                && territoryRules.CanEnter(EnemyKind.Dokkaebi, context.PlayerTerritory))
            {
                return new ThreatDirectorDecision(
                    ThreatDirectorAction.SpawnDokkaebi,
                    EnemyKind.Dokkaebi,
                    profile,
                    context.ActiveDokkaebiCount == 0 ? "forest_dokkaebi" : "forest_dokkaebi_reinforcement");
            }

            return Cue(profile, "forest_pressure_cue");
        }

        private ThreatDirectorDecision EvaluateEstateInterior(
            ThreatDirectorContext context,
            ThreatStageProfile profile)
        {
            if (profile.Stage >= 4
                && context.ActiveGhostCount < profile.MaxActiveThreats
                && territoryRules.CanEnter(EnemyKind.Ghost, context.PlayerTerritory))
            {
                return new ThreatDirectorDecision(
                    ThreatDirectorAction.SpawnGhost,
                    EnemyKind.Ghost,
                    profile,
                    context.ActiveGhostCount == 0 ? "interior_ghost" : "interior_ghost_reinforcement");
            }

            return Cue(profile, "interior_pressure_cue");
        }

        private static ThreatDirectorDecision None(ThreatStageProfile profile, string reason)
        {
            return new ThreatDirectorDecision(ThreatDirectorAction.None, null, profile, reason);
        }

        private static ThreatDirectorDecision Cue(ThreatStageProfile profile, string reason)
        {
            return new ThreatDirectorDecision(ThreatDirectorAction.CueOnly, null, profile, reason);
        }
    }
}
