using Script.Manager;

namespace Script.Model
{
    using System;
    using Script.Combat;
    using UnityEngine;

    public abstract class Character : Entity, IDamageable
    {
        [SerializeField, Min(1)] private int maxHealth = 100;

        protected int Row;
        protected float Height;

        private Health _health;

        public int MaxHealth => RuntimeHealth.Maximum;
        public int CurrentHealth => RuntimeHealth.Current;
        public bool IsDead => RuntimeHealth.IsDead;

        public event Action<DamageInfo, DamageResult> Damaged;
        public event Action<DamageInfo> Died;
        public event Action<int> Healed;

        private Health RuntimeHealth => _health ??= new Health(Mathf.Max(1, maxHealth));

        public Character SetRow(int row)
        {
            Row = row;
            return this;
        }
        
        public Character SetHeight(float height)
        {
            Height = height;
            return this;
        }

        public DamageResult TakeDamage(DamageInfo damage)
        {
            var result = RuntimeHealth.ApplyDamage(damage.Amount);
            if (result.WasIgnored) return result;

            OnDamaged(damage, result);
            Damaged?.Invoke(damage, result);
            if (result.WasLethal)
            {
                Died?.Invoke(damage);
                OnDied(damage);
            }
            return result;
        }

        public int Heal(int amount)
        {
            var appliedAmount = RuntimeHealth.Heal(amount);
            if (appliedAmount > 0)
            {
                OnHealed(appliedAmount);
                Healed?.Invoke(appliedAmount);
            }
            return appliedAmount;
        }

        public void ResetHealth()
        {
            RuntimeHealth.Reset(Mathf.Max(1, maxHealth));
        }

        protected virtual void OnDamaged(DamageInfo damage, DamageResult result)
        {
        }

        protected virtual void OnHealed(int amount)
        {
        }

        protected virtual void OnDied(DamageInfo killingBlow)
        {
            Destroy(gameObject);
        }

    }
}
