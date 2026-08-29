using System;
using PvZ.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace PvZ.Gameplay.Sun
{
    /// <summary>Owns only the player's current sunlight balance.</summary>
    public sealed class SunWallet : SceneSingleton<SunWallet>
    {
        [FormerlySerializedAs("initialSunlight")]
        [SerializeField, Min(0)] private int initialBalance = 100;

        public int Balance { get; private set; }

        public event Action<int> BalanceChanged;

        public void SetBalance(int amount)
        {
            Balance = Mathf.Max(0, amount);
            BalanceChanged?.Invoke(Balance);
        }

        public bool CanAfford(int amount) => amount >= 0 && Balance >= amount;

        public bool TrySpend(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!CanAfford(amount)) return false;

            SetBalance(Balance - amount);
            return true;
        }

        public void Deposit(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            SetBalance(Balance + amount);
        }

        protected override void OnReferencesValidated()
        {
            SetBalance(initialBalance);
        }
    }
}
