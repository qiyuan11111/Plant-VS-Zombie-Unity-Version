using System.Collections.Generic;
using PvZ.Bootstrap;
using PvZ.Config;
using PvZ.Core;
using PvZ.Gameplay.Sun;
using UnityEngine;

namespace PvZ.Gameplay.SeedCards
{
    public sealed class SeedBankController : SceneSingleton<SeedBankController>
    {
        [SerializeField] private SeedCardLoadout loadout = new();
        [SerializeField] private RectTransform seedBankRectTransform;
        [SerializeField] private RectTransform cardGroupRectTransform;

        private readonly List<SeedCard> _seedCards = new();
        private readonly List<PlantDefinition> _plantDefinitions = new();
        private int _currentSunlight;

        public void SetCurrentSunlight(int sunlight)
        {
            _currentSunlight = sunlight;
            foreach (var seedCard in _seedCards)
            {
                seedCard.SetCurrentSunlight(sunlight);
            }
        }

        private SeedCard CreateSeedCard(PlantDefinition plantDefinition, string cardName)
        {
            var seedCard = Instantiate(GameBootstrap.Instance.Catalog.GetObjectByType(ObjectType.Card),
                cardGroupRectTransform, true).GetComponent<SeedCard>();
            seedCard.Initialize(plantDefinition);
            seedCard.SetCurrentSunlight(_currentSunlight);
            seedCard.SetName(cardName);
            return seedCard;
        }

        private void InitSeedCardGroup()
        {
            _seedCards.Clear();

            var cardCount = _plantDefinitions.Count;
            ApplyLayout(cardCount);

            for (var i = 0; i < cardCount; i++)
            {
                var plantDefinition = _plantDefinitions[i];
                var cardName = "CardItem-" + i;
                _seedCards.Add(CreateSeedCard(plantDefinition, cardName));
            }
        }

        private void ApplyLayout(int cardCount)
        {
            var layout = SeedBankLayout.Calculate(cardCount);
            seedBankRectTransform.sizeDelta =
                new Vector2(layout.Width, seedBankRectTransform.sizeDelta.y);
            cardGroupRectTransform.offsetMax =
                new Vector2(layout.GroupRightOffset, cardGroupRectTransform.offsetMax.y);
        }

        public void LoadPlantCard(int index, PlantType type)
        {
            if (index >= 0 && index < _plantDefinitions.Count)
            {
                _plantDefinitions[index] = GameBootstrap.Instance.Catalog.GetPlantDefinition(type);
            }
        }

        private void LoadCardGroup()
        {
            _plantDefinitions.Clear();
            foreach (var type in loadout.PlantTypes)
            {
                _plantDefinitions.Add(GameBootstrap.Instance.Catalog.GetPlantDefinition(type));
            }

            InitSeedCardGroup();
        }

        private void StartInitialCooldown()
        {
            foreach (var seedCard in _seedCards)
            {
                seedCard.StartCooldown();
            }
        }

        protected override void OnSingletonAwake()
        {
            seedBankRectTransform = GetComponent<RectTransform>();
            var cardGroup = seedBankRectTransform != null ? seedBankRectTransform.Find("Group") : null;
            cardGroupRectTransform = cardGroup != null ? cardGroup.GetComponent<RectTransform>() : null;
        }

        protected override bool ValidateReferences()
        {
            var isValid = true;
            if (loadout == null)
            {
                Debug.LogError($"{nameof(SeedBankController)} requires a loadout.", this);
                isValid = false;
            }
            isValid &= RequireReference(seedBankRectTransform, $"a {nameof(RectTransform)} on the same GameObject");
            isValid &= RequireReference(cardGroupRectTransform, "a child RectTransform named Group");
            return isValid;
        }

        protected override bool ValidateDependencies()
        {
            var valid = true;
            valid &= RequireManager(GameBootstrap.Instance);
            valid &= RequireManager(SunWallet.Instance);
            return valid;
        }

        protected override void OnSingletonStart()
        {
            _currentSunlight = SunWallet.Instance.Balance;
            SunWallet.Instance.BalanceChanged += SetCurrentSunlight;

            LoadCardGroup();
            StartInitialCooldown();
        }

        protected override void OnSingletonDestroy()
        {
            if (SunWallet.Instance != null)
            {
                SunWallet.Instance.BalanceChanged -= SetCurrentSunlight;
            }
        }
    }
}
