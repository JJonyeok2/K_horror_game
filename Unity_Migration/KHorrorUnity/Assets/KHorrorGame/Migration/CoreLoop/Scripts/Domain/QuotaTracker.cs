using System;

namespace KHorrorGame.Migration
{
    [Serializable]
    public sealed class QuotaTracker
    {
        public int RequiredValue { get; private set; }
        public int RecoveredValue { get; private set; }
        public int Debt { get; private set; }
        public int FailedQuotaChecks { get; private set; }

        public QuotaTracker(int startingRequiredValue = 1000)
        {
            RequiredValue = Math.Max(startingRequiredValue, 0);
        }

        public void AddRecoveredValue(int value)
        {
            RecoveredValue += Math.Max(value, 0);
        }

        public bool IsQuotaMet()
        {
            return RecoveredValue >= RequiredValue;
        }

        public bool CloseQuotaCheck()
        {
            if (IsQuotaMet())
            {
                return true;
            }

            Debt += RequiredValue - RecoveredValue;
            FailedQuotaChecks += 1;
            return false;
        }

        public bool IsContractEnded()
        {
            return FailedQuotaChecks >= 3;
        }
    }
}
