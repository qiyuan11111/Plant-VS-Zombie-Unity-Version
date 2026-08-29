namespace PvZ.Core.Combat
{
    public readonly struct DamageResult
    {
        public DamageResult(int requestedAmount, int appliedAmount, int remainingHealth, bool wasLethal)
        {
            RequestedAmount = requestedAmount;
            AppliedAmount = appliedAmount;
            RemainingHealth = remainingHealth;
            WasLethal = wasLethal;
        }

        public int RequestedAmount { get; }
        public int AppliedAmount { get; }
        public int RemainingHealth { get; }
        public bool WasLethal { get; }
        public bool WasIgnored => AppliedAmount == 0;
    }
}
