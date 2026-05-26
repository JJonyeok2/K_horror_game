using System;
using System.Collections.Generic;
using System.Linq;

namespace KHorrorGame.Migration
{
    [Serializable]
    public sealed class Inventory
    {
        private readonly List<ArtifactDefinition> _items = new List<ArtifactDefinition>();

        public float MaxWeight { get; private set; }
        public int MaxHandSlots { get; private set; }
        public IReadOnlyList<ArtifactDefinition> Items
        {
            get { return _items; }
        }

        public Inventory(float maxWeight = 10f, int maxHandSlots = 2)
        {
            MaxWeight = Math.Max(maxWeight, 0f);
            MaxHandSlots = Math.Max(maxHandSlots, 0);
        }

        public bool TryAdd(ArtifactDefinition item)
        {
            if (item == null)
            {
                return false;
            }

            if (item.HandSlots > FreeHandSlots())
            {
                return false;
            }

            _items.Add(item);
            return true;
        }

        public void Clear()
        {
            _items.Clear();
        }

        public ArtifactDefinition PopLastItem()
        {
            if (_items.Count == 0)
            {
                return null;
            }

            var index = _items.Count - 1;
            var item = _items[index];
            _items.RemoveAt(index);
            return item;
        }

        public int TotalValue()
        {
            return _items.Sum(item => item.Value);
        }

        public int TotalResentmentGain()
        {
            return _items.Sum(item => item.ResentmentGain);
        }

        public float TotalWeight()
        {
            return _items.Sum(item => item.Weight);
        }

        public int UsedHandSlots()
        {
            return _items.Sum(item => item.HandSlots);
        }

        public int FreeHandSlots()
        {
            return Math.Max(MaxHandSlots - UsedHandSlots(), 0);
        }

        public string HandStatus()
        {
            if (_items.Count == 0)
            {
                return "Hands empty";
            }

            return string.Format(
                "Hands {0}/{1}: {2}",
                UsedHandSlots(),
                MaxHandSlots,
                string.Join(" / ", _items.Select(item => item.DisplayName).ToArray()));
        }
    }
}
