using System;
using System.Collections.Generic;

namespace KHorrorGame.Migration
{
    public sealed class EnemyTerritoryRules
    {
        private static readonly IReadOnlyList<TerritoryKind> EmptyTerritories =
            Array.AsReadOnly(new TerritoryKind[0]);

        private static readonly IReadOnlyList<TerritoryKind> GhostTerritories =
            Array.AsReadOnly(new[] { TerritoryKind.EstateInterior });

        private static readonly IReadOnlyList<TerritoryKind> DokkaebiTerritories =
            Array.AsReadOnly(new[] { TerritoryKind.ForestApproach });

        public static EnemyTerritoryRules Default { get; } = new EnemyTerritoryRules();

        public bool CanEnter(EnemyKind enemyKind, TerritoryKind territoryKind)
        {
            var allowedTerritories = AllowedTerritoriesFor(enemyKind);
            for (var i = 0; i < allowedTerritories.Count; i++)
            {
                if (allowedTerritories[i] == territoryKind)
                {
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<TerritoryKind> AllowedTerritoriesFor(EnemyKind enemyKind)
        {
            switch (enemyKind)
            {
                case EnemyKind.Ghost:
                    return GhostTerritories;
                case EnemyKind.Dokkaebi:
                    return DokkaebiTerritories;
                default:
                    return EmptyTerritories;
            }
        }
    }
}
