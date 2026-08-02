using System;
using Script.Model;

namespace Script.Combat
{
    public enum DamageType
    {
        Normal,
        Projectile,
        Bite,
        Explosion
    }

    public readonly struct DamageInfo
    {
        public int Amount { get; }
        public DamageType Type { get; }
        public Entity Source { get; }

        public DamageInfo(int amount, DamageType type = DamageType.Normal, Entity source = null)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));

            Amount = amount;
            Type = type;
            Source = source;
        }
    }

    public interface IDamageable
    {
        int MaxHealth { get; }
        int CurrentHealth { get; }
        bool IsDead { get; }

        DamageResult TakeDamage(DamageInfo damage);
    }
}
