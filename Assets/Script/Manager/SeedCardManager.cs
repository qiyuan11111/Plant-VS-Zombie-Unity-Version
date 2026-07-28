using System;
using System.Collections.Generic;
using Prefab.Object.SeedCard.Script;
using Script.Util;
using UnityEngine;

namespace Script.Manager
{
    public class SeedCardManager : MonoBehaviour
    {
        public static SeedCardManager Instance;
        private readonly List<OnFieldSeedCard> _seedCards = new();
        private readonly List<GameConfigObject.PlantType> _plantTypes = new();

        public RectTransform seedBankRectTransform;
        public RectTransform cardGroupRectTransform;

        public OnFieldSeedCard GetCard(int index)
        {
            if (index < 0 || index >= _seedCards.Count) return null;
            return _seedCards[index];
        }

        public List<GameConfigObject.PlantType> GetPlantTypes()
        {
            return _plantTypes;
        }

        public GameConfigObject.PlantType GetPlantType(int index)
        {
            return _plantTypes[index];
        }

        public bool IsPlantable(int index)
        {
            return _seedCards[index].IsPlantable();
        }

        public void CancelChoosePlantCard(int index)
        {
            if (index >= 0 && index < _seedCards.Count)
            {
                _seedCards[index].CancelChoose();
            }
        }

        public void ChoosePlantCard(int index)
        {
            if (index >= 0 && index < _seedCards.Count)
            {
                _seedCards[index].OnChoose();
            }
        }

        public void AfterPlacePlant(int index)
        {
            if (index >= 0 && index < _seedCards.Count)
            {
                _seedCards[index].AfterPlace();
            }
        }

        public void UpdateCardGroupState()
        {
            foreach (var seedCard in _seedCards)
            {
                seedCard.UpdateState();
            }
        }

        private const float SeedBankWidth = 444;
        private const float CardWidth = 50;

        private OnFieldSeedCard CreateSeedCard(GameConfigObject.PlantType plantType, string cardName)
        {
            var seedCard = Instantiate(MainGameManager.Instance.GetObjectByType(GameConfigObject.ObjectType.Card),
                cardGroupRectTransform, true).GetComponent<SeedCard>();
            seedCard.AfterCreate(new Dictionary<string, object>
            {
                { "entityType", plantType }
            });

            return seedCard.ToField<OnFieldSeedCard>(new Dictionary<string, object>
                {
                    { "Name", cardName }
                });
        }

        private void InitSeedCardGroup()
        {
            _seedCards.Clear();

            var num = _plantTypes.Count;
            InitSeedBankSize(num);

            for (var i = 0; i < num; i++)
            {
                var plantType = _plantTypes[i];
                var cardName = "CardItem-" + i;
                _seedCards.Add(CreateSeedCard(plantType, cardName));
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
            _plantTypes.Add(type);
        }

        public void LoadPlantCard(int index, GameConfigObject.PlantType type)
        {
            if (index >= 0 && index < _plantTypes.Count)
            {
                _plantTypes[index] = type;
            }
        }

        private void ConfirmChosenPlant()
        {
            PlantingManager.Instance.CreateMousePlant(_plantTypes);
            PlantingManager.Instance.ResetMousePlantPosition();

            PlantingManager.Instance.CreateGridPlant(_plantTypes);
            PlantingManager.Instance.ResetGridPlantPosition();
        }

        private void ConfirmChosenPlant(int index)
        {
            PlantingManager.Instance.CreateMousePlant(_plantTypes[index], index);
            PlantingManager.Instance.ResetMousePlantPosition();

            PlantingManager.Instance.CreateGridPlant(_plantTypes[index], index);
            PlantingManager.Instance.ResetGridPlantPosition();
        }

        private void LoadCardGroup()
        {
            LoadPlantCard(GameConfigObject.PlantType.SunShroom);
            LoadPlantCard(GameConfigObject.PlantType.SunShroom);
            LoadPlantCard(GameConfigObject.PlantType.SunShroom);
            LoadPlantCard(GameConfigObject.PlantType.SunShroom);
            LoadPlantCard(GameConfigObject.PlantType.SunShroom);
            LoadPlantCard(GameConfigObject.PlantType.SunShroom);
            LoadPlantCard(GameConfigObject.PlantType.SunShroom);
            // LoadPlantCard(GameConfigObject.PlantType.PeaShooterSingle);
            ConfirmChosenPlant();

            InitSeedCardGroup();
        }

        private void StartColdDown()
        {
            foreach (var seedCard in _seedCards)
            {
                seedCard.ColdDown(seedCard.GetCdTime());
            }
        }

        private void StartColdDown(int index)
        {
            if (index >= 0 && index < _seedCards.Count)
            {
                _seedCards[index].ColdDown(_seedCards[index].GetCdTime());
            }
        }
        // public List<GameConfigObject.PlantType> GetChosenEntityTypes()
        // {
        //     return _plantTypes;
        // }

        private void Awake()
        {
            Instance = this;

            seedBankRectTransform = GetComponent<RectTransform>();
            cardGroupRectTransform = seedBankRectTransform.Find("Group").GetComponent<RectTransform>();
        }

        private void Start()
        {
            LoadCardGroup();
            StartColdDown();
        }
    }
}