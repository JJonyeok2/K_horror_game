using NUnit.Framework;

namespace KHorrorGame.Migration.Tests
{
    public sealed class EnemyTerritoryRulesTests
    {
        [Test]
        public void GhostCanOnlyEnterEstateInterior()
        {
            var rules = EnemyTerritoryRules.Default;

            Assert.IsTrue(rules.CanEnter(EnemyKind.Ghost, TerritoryKind.EstateInterior));
            Assert.IsFalse(rules.CanEnter(EnemyKind.Ghost, TerritoryKind.ForestApproach));
            Assert.IsFalse(rules.CanEnter(EnemyKind.Ghost, TerritoryKind.BongoHub));
            Assert.IsFalse(rules.CanEnter(EnemyKind.Ghost, TerritoryKind.SettlementOffice));
            CollectionAssert.AreEquivalent(
                new[] { TerritoryKind.EstateInterior },
                rules.AllowedTerritoriesFor(EnemyKind.Ghost));
        }

        [Test]
        public void DokkaebiCanOnlyEnterForestApproach()
        {
            var rules = EnemyTerritoryRules.Default;

            Assert.IsTrue(rules.CanEnter(EnemyKind.Dokkaebi, TerritoryKind.ForestApproach));
            Assert.IsFalse(rules.CanEnter(EnemyKind.Dokkaebi, TerritoryKind.EstateInterior));
            Assert.IsFalse(rules.CanEnter(EnemyKind.Dokkaebi, TerritoryKind.BongoHub));
            Assert.IsFalse(rules.CanEnter(EnemyKind.Dokkaebi, TerritoryKind.SettlementOffice));
            CollectionAssert.AreEquivalent(
                new[] { TerritoryKind.ForestApproach },
                rules.AllowedTerritoriesFor(EnemyKind.Dokkaebi));
        }

        [Test]
        public void UnknownEnemyKindCannotEnterAnyTerritory()
        {
            var rules = EnemyTerritoryRules.Default;
            var unknownKind = (EnemyKind)999;

            Assert.IsFalse(rules.CanEnter(unknownKind, TerritoryKind.EstateInterior));
            Assert.IsFalse(rules.CanEnter(unknownKind, TerritoryKind.ForestApproach));
            Assert.IsEmpty(rules.AllowedTerritoriesFor(unknownKind));
        }
    }
}
