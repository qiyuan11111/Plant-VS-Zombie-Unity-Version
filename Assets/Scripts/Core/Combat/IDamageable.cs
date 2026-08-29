namespace PvZ.Core.Combat
{
    public interface IDamageable
    {
        int MaxHealth { get; }
        int CurrentHealth { get; }
        bool IsDead { get; }
        DamageResult TakeDamage(DamageInfo damage);
    }
}
