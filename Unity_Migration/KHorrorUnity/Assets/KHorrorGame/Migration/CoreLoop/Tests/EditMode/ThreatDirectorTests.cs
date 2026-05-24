using NUnit.Framework;

namespace KHorrorGame.Migration.Tests
{
    public sealed class ThreatDirectorTests
    {
        [Test]
        public void NonEstateMapsNeverSpawnThreats()
        {
            var director = new ThreatDirector();

            var decision = director.Evaluate(new ThreatDirectorContext(
                GameMapId.BongoHub,
                TerritoryKind.BongoHub,
                5,
                true,
                0,
                0,
                0));

            Assert.AreEqual(ThreatDirectorAction.None, decision.Action);
            Assert.IsNull(decision.EnemyKind);
            Assert.AreEqual("not_estate", decision.Reason);
        }

        [Test]
        public void ShrineGraceSuppressesAggressiveSpawnButKeepsCue()
        {
            var director = new ThreatDirector();

            var decision = director.Evaluate(new ThreatDirectorContext(
                GameMapId.JonggaEstate,
                TerritoryKind.EstateInterior,
                5,
                false,
                0,
                0,
                0));

            Assert.AreEqual(ThreatDirectorAction.CueOnly, decision.Action);
            Assert.IsNull(decision.EnemyKind);
            Assert.AreEqual("grace_or_gate_blocked", decision.Reason);
        }

        [Test]
        public void ForestStageThreeRequestsDokkaebi()
        {
            var director = new ThreatDirector();

            var decision = director.Evaluate(new ThreatDirectorContext(
                GameMapId.JonggaEstate,
                TerritoryKind.ForestApproach,
                3,
                true,
                0,
                0,
                11));

            Assert.AreEqual(ThreatDirectorAction.SpawnDokkaebi, decision.Action);
            Assert.AreEqual(EnemyKind.Dokkaebi, decision.EnemyKind);
            Assert.AreEqual(3, decision.Profile.Stage);
        }

        [Test]
        public void EstateStageFourRequestsGhost()
        {
            var director = new ThreatDirector();

            var decision = director.Evaluate(new ThreatDirectorContext(
                GameMapId.JonggaEstate,
                TerritoryKind.EstateInterior,
                4,
                true,
                0,
                0,
                23));

            Assert.AreEqual(ThreatDirectorAction.SpawnGhost, decision.Action);
            Assert.AreEqual(EnemyKind.Ghost, decision.EnemyKind);
            Assert.AreEqual(4, decision.Profile.Stage);
        }

        [Test]
        public void FullStageBudgetBlocksAdditionalSpawns()
        {
            var director = new ThreatDirector();
            var profile = ThreatStageProfile.ForStage(5);

            var decision = director.Evaluate(new ThreatDirectorContext(
                GameMapId.JonggaEstate,
                TerritoryKind.EstateInterior,
                5,
                true,
                profile.MaxActiveThreats,
                0,
                0));

            Assert.AreEqual(ThreatDirectorAction.None, decision.Action);
            Assert.IsNull(decision.EnemyKind);
            Assert.AreEqual("budget_full", decision.Reason);
        }

        [Test]
        public void StageProfilesEscalateMonotonically()
        {
            var previous = ThreatStageProfile.ForStage(0);

            for (var stage = 1; stage <= ThreatStageProfile.MaxStage; stage++)
            {
                var current = ThreatStageProfile.ForStage(stage);

                Assert.GreaterOrEqual(current.Stage, previous.Stage);
                Assert.GreaterOrEqual(current.MaxActiveThreats, previous.MaxActiveThreats);
                Assert.GreaterOrEqual(current.DamagePerHit, previous.DamagePerHit);
                Assert.GreaterOrEqual(current.AttackRange, previous.AttackRange);
                Assert.GreaterOrEqual(current.PursuitSpeed, previous.PursuitSpeed);
                Assert.GreaterOrEqual(current.PatternVariance, previous.PatternVariance);

                if (previous.AttackIntervalSeconds > 0f && current.AttackIntervalSeconds > 0f)
                {
                    Assert.LessOrEqual(current.AttackIntervalSeconds, previous.AttackIntervalSeconds);
                }

                previous = current;
            }
        }
    }
}
