using System;
using System.Collections.Generic;

namespace KHorrorGame.Migration
{
    [Serializable]
    public sealed class ArtifactDefinition
    {
        private readonly List<string> _tags;

        public string DisplayName { get; private set; }
        public int Value { get; private set; }
        public float Weight { get; private set; }
        public int ResentmentGain { get; private set; }
        public IReadOnlyList<string> Tags
        {
            get { return _tags; }
        }
        public int HandSlots { get; private set; }

        public ArtifactDefinition(
            string displayName = "",
            int value = 0,
            float weight = 0f,
            int resentmentGain = 0,
            IEnumerable<string> tags = null,
            int handSlots = 1)
        {
            DisplayName = displayName ?? string.Empty;
            Value = Math.Max(value, 0);
            Weight = Math.Max(weight, 0f);
            ResentmentGain = Math.Max(resentmentGain, 0);
            HandSlots = ClampInt(handSlots, 1, 2);
            _tags = tags == null ? new List<string>() : new List<string>(tags);
        }

        public bool HasTag(string tag)
        {
            return !string.IsNullOrEmpty(tag) && _tags.Contains(tag);
        }

        private static int ClampInt(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }
    }
}
