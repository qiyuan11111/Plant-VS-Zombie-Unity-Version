using System;

namespace PvZ.Core.Combat
{
    public readonly struct DamageResult
    {
        public int RequestedAmount { get; }
        public int AppliedAmount { get; }
        public int RemainingHealth { get; }
        public bool WasLethal { get; }
        public bool WasIgnored => AppliedAmount == 0;

        public DamageResult(int requestedAmount, int appliedAmount, int remainingHealth, bool wasLethal)
        {
            RequestedAmount = requestedAmount;
            AppliedAmount = appliedAmount;
            RemainingHealth = remainingHealth;
            WasLethal = wasLethal;
        }
    }

    /// <summary>
    /// Engine-independent health state. Unity-facing entities delegate damage handling to this class.
    /// </summary>
    public sealed class Health
    {
        public int Maximum { get; private set; }
        public int Current { get; private set; }
        public bool IsDead => Current == 0;

        public Health(int maximum)
        {
            Reset(maximum);
        }

        public void Reset(int maximum)
        {
            if (maximum <= 0) throw new ArgumentOutOfRangeException(nameof(maximum));

            Maximum = maximum;
            Current = maximum;
        }

        public DamageResult ApplyDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0 || IsDead)
            {
                return new DamageResult(amount, 0, Current, false);
            }

            var appliedAmount = Math.Min(Current, amount);
            Current -= appliedAmount;
            return new DamageResult(amount, appliedAmount, Current, Current == 0);
        }

        public int Heal(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0 || IsDead) return 0;

            var appliedAmount = Math.Min(Maximum - Current, amount);
            Current += appliedAmount;
            return appliedAmount;
        }
    }
}
