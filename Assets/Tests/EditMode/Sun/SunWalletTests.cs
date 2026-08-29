using System;
using NUnit.Framework;
using PvZ.Gameplay.Sun;
using UnityEngine;

namespace PvZ.Tests.EditMode.Sun
{
    public sealed class SunWalletTests
    {
        private GameObject _gameObject;

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null) UnityEngine.Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void Wallet_InitializesAndPublishesBalanceChanges()
        {
            var wallet = CreateWallet();
            var publishedBalance = -1;
            wallet.BalanceChanged += balance => publishedBalance = balance;

            wallet.Deposit(25);

            Assert.That(wallet.Balance, Is.EqualTo(125));
            Assert.That(publishedBalance, Is.EqualTo(125));
            Assert.That(wallet.CanAfford(125), Is.True);
            Assert.That(wallet.CanAfford(126), Is.False);
        }

        [Test]
        public void TrySpend_DoesNotMutateBalanceWhenFundsAreInsufficient()
        {
            var wallet = CreateWallet();

            Assert.That(wallet.TrySpend(101), Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(100));
            Assert.That(wallet.TrySpend(40), Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(60));
        }

        [Test]
        public void NegativeTransactions_AreRejected()
        {
            var wallet = CreateWallet();

            Assert.Throws<ArgumentOutOfRangeException>(() => wallet.Deposit(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => wallet.TrySpend(-1));
            Assert.That(wallet.Balance, Is.EqualTo(100));
        }

        private SunWallet CreateWallet()
        {
            _gameObject = new GameObject("SunWalletTests");
            return _gameObject.AddComponent<SunWallet>();
        }
    }
}
