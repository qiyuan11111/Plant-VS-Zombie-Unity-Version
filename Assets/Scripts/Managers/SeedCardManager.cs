using System;
using Prefab.Object.SeedCard.Script;
using System.Collections.Generic;
using Script.Util;
using UnityEngine;

namespace Script.Manager
{
    public class SeedCardManager : SceneSingleton<SeedCardManager>
    {
        private readonly List<SeedCard> _seedCards = new();
        private readonly List<PlantDefinition> _plantDefinitions = new();
        private int _currentSunlight;

        public RectTransform seedBankRectTransform;
        public RectTransform cardGroupRectTransform;

        public void SetCurrentSunlight(int sunlight)
        {
            _currentSunlight = sunlight;
            foreach (var seedCard in _seedCards)
            {
                seedCard.SetCurrentSunlight(sunlight);
            }
        }

        private const float SeedBankWidth = 444;
        private const float CardWidth = 50;

        private SeedCard CreateSeedCard(PlantDefinition plantDefinition, string cardName)
        {
            var seedCard = Instantiate(MainGameManager.Instance.GetObjectByType(GameConfigObject.ObjectType.Card),
                cardGroupRectTransform, true).GetComponent<SeedCard>();
            seedCard.Initialize(plantDefinition);
            seedCard.SetCurrentSunlight(_currentSunlight);
            seedCard.SetName(cardName);
            return seedCard;
        }

        private void InitSeedCardGroup()
        {
            _seedCards.Clear();

            var num = _plantDefinitions.Count;
            InitSeedBankSize(num);

            for (var i = 0; i < num; i++)
            {
                var plantDefinition = _plantDefinitions[i];
                var cardName = "CardItem-" + i;
                _seedCards.Add(CreateSeedCard(plantDefinition, cardName));
            }
        }

        private void InitSeedBankSize(int num)
        {
            if (num < 6)
            {
                seedBankRectTransform.sizeDelta = new Vector2(SeedBankWidth, seedBankRectTransform.sizeDelta.y);
                cardGroupRectTransform.offsetMax =
                    new Vector2((6 - num) * (CardWidth + 9) + 15, cardGroupRectTransform.offsetMax.y);
            }
            else
            {
                seedBankRectTransform.sizeDelta =
                    new Vector2(SeedBankWidth + (num - 6) * (CardWidth + 9), seedBankRectTransform.sizeDelta.y);
                cardGroupRectTransform.offsetMax = new Vector2(-15, cardGroupRectTransform.offsetMax.y);
            }
        }

        private void LoadPlantCard(GameConfigObject.PlantType type)
        {
            _plantDefinitions.Add(MainGameManager.Instance.GetPlantDefinition(type));
        }

        public void LoadPlantCard(int index, GameConfigObject.PlantType type)
        {
            if (index >= 0 && index < _plantDefinitions.Count)
            {
                _plantDefinitions[index] = MainGameManager.Instance.GetPlantDefinition(type);
            }
        }

        private void LoadCardGroup()
        {
            LoadPlantCard(GameConfigObject.PlantType.SunFlower);
            LoadPlantCard(GameConfigObject.PlantType.SunShroom);
            LoadPlantCard(GameConfigObject.PlantType.SunShroom);
            LoadPlantCard(GameConfigObject.PlantType.SunShroom);
            LoadPlantCard(GameConfigObject.PlantType.SunShroom);
            LoadPlantCard(GameConfigObject.PlantType.SunShroom);
            LoadPlantCard(GameConfigObject.PlantType.PeaShooterSingle);
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
            isValid &= RequireReference(seedBankRectTransform, $"a {nameof(RectTransform)} on the same GameObject");
            isValid &= RequireReference(cardGroupRectTransform, "a child RectTransform named Group");
            return isValid;
        }

        protected override bool ValidateDependencies()
        {
            return RequireManager(MainGameManager.Instance);
        }

        protected override void OnSingletonStart()
        {
            if (SunManager.Instance != null)
            {
                _currentSunlight = SunManager.Instance.GetCurrentSunLight();
            }

            LoadCardGroup();
            StartInitialCooldown();
        }
    }
}
