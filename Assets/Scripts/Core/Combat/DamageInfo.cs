using System;
using PvZ.Gameplay.Entities;

namespace PvZ.Core.Combat
{
    public readonly struct DamageInfo
    {
        public DamageInfo(int amount, DamageType type = DamageType.Normal, GameEntity source = null)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Amount = amount;
            Type = type;
            Source = source;
        }

        public int Amount { get; }
        public DamageType Type { get; }
        public GameEntity Source { get; }
    }
}
