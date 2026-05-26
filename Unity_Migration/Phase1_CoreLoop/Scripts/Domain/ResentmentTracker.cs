using System;
using System.Collections.Generic;

namespace KHorrorGame.Migration
{
    public sealed class ResentmentChangedEventArgs : EventArgs
    {
        public int Value { get; private set; }
        public int Stage { get; private set; }
        public string Reason { get; private set; }

        public ResentmentChangedEventArgs(int value, int stage, string reason)
        {
            Value = value;
            Stage = stage;
            Reason = reason ?? string.Empty;
        }
    }

    [Serializable]
    public sealed class ResentmentTracker
    {
        private readonly List<string> _history = new List<string>();

        public event EventHandler<ResentmentChangedEventArgs> ResentmentChanged;

        public int CurrentValue { get; private set; }
        public IReadOnlyList<string> History
        {
            get { return _history; }
        }

        public void AddResentment(int amount, string reason)
        {
            CurrentValue += Math.Max(amount, 0);
            _history.Add(reason ?? string.Empty);
            var handler = ResentmentChanged;
            if (handler != null)
            {
                handler(this, new ResentmentChangedEventArgs(CurrentValue, Stage(), reason));
            }
        }

        public int Stage()
        {
            if (CurrentValue <= 0)
            {
                return 0;
            }

            if (CurrentValue <= 2)
            {
                return 1;
            }

            if (CurrentValue <= 4)
            {
                return 2;
            }

            if (CurrentValue <= 7)
            {
                return 3;
            }

            if (CurrentValue <= 10)
            {
                return 4;
            }

            return 5;
        }
    }
}
