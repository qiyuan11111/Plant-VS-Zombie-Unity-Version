using System;
using System.Collections.Generic;
using System.Linq;
using Script.Model;
using Script.Util;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Script.Manager
{
    public class PlantingManager : MonoBehaviour, IPointerClickHandler
    {
        public static PlantingManager Instance;
        
        private readonly List<OnFieldEntity> _mouseEntity = new(); //准备种植时，跟随鼠标
        
        private readonly List<OnFieldEntity> _gridEntity = new(); // 准备种植时，草坪上的虚影

        private Transform _plantTransform;
        
        private Vector2Int _currentChosenPoint; // 草坪行列
        
        // private readonly List<bool> _isSeedCardChosen = new();
        
        private int _currentChosenCardIndex;
        public int GetCurrentChosenCardIndex()
        {
            return _currentChosenCardIndex;
        }

        public void SetCurrentChosenCardIndex(int index)
        {
            _currentChosenCardIndex = index;
            if (index != -1)
            {
                SeedCardManager.Instance.ChoosePlantCard(index);
            }
        }
        
        public bool IsChooseCard()
        {
            return _currentChosenCardIndex != -1;
        }
        
        private int _currentPutOnIndex = -1;
        public void SetCurrentPutOnCardIndex(int currentPutOnIndex)
        {
            _currentPutOnIndex = currentPutOnIndex;
        }

        public bool IsPutOnCard()
        {
            return _currentPutOnIndex != -1;
        }

        public int GetCurrentPutOnCardIndex()
        {
            return _currentPutOnIndex;
        }
        
        private void DoChoosePlantCard(int cardIndex)
        {
            MainGameManager.Instance.SetMouseStatus(MainGameManager.MouseEvent.Planting);
            SoundManager.Instance.PlayEffect(GameSound.SoundType.SeedLift);
            SetCurrentChosenCardIndex(cardIndex);
        }
        
        public void ChoosePlantCard()
        {
            if (IsChooseCard())
            {
                CancelChoosePlantCard();
                return;
            }
            var index = GetCurrentPutOnCardIndex();
            if (!IsPutOnCard() || !SeedCardManager.Instance.IsPlantable(index))
            {
                return;
            }
            DoChoosePlantCard(index);
        }

        private void DoCancelChoosePlant(int cardIndex)
        {
            MainGameManager.Instance.SetMouseStatus(MainGameManager.MouseEvent.None);
            SetCurrentChosenCardIndex(-1);
            SeedCardManager.Instance.CancelChoosePlantCard(cardIndex);
        }
        
        public void CancelChoosePlantCard()
        {
            SetCurrentChosenPoint(GridManager.Grid.None);
            if (!IsChooseCard())
            {
                return;
            }
            DoCancelChoosePlant(GetCurrentChosenCardIndex());
        }
        
        
        public void CreateGridPlant(List<GameConfigObject.PlantType> plantTypes)
        {
            _gridEntity.Clear();
            foreach (var onFieldPlant in plantTypes.Select(type => Instantiate(MainGameManager.Instance.GetPlantByType(type),
                         GridManager.Instance.transform, true).GetComponent<Plant>()).Select(plant => plant.ToField()))
            {
                _gridEntity.Add(onFieldPlant.SetGridIconMode());
            }
        }
        
        public void CreateGridPlant(GameConfigObject.PlantType plantTypes, int index)
        {
            var plant = Instantiate(MainGameManager.Instance.GetPlantByType(plantTypes), GridManager.Instance.transform, true)
                .GetComponent<Plant>();
            var onFieldPlant = plant.ToField();
            if (onFieldPlant == null) return;
            _gridEntity[index] = onFieldPlant.SetGridIconMode();
        }
        
        private void DoResetGridPlantPosition(Vector3 position)
        {
            foreach (var onFieldEntity in _gridEntity)
            {
                onFieldEntity.SetLocalPosition(position);
            }
        }
        
        public void ResetGridPlantPosition()
        {
            var position = new Vector3(1000f, 1000f, 1000f);
            DoResetGridPlantPosition(position);
        }
        
        public void CreateMousePlant(List<GameConfigObject.PlantType> plantTypes)
        {
            _mouseEntity.Clear();
            foreach (var onFieldPlant in plantTypes.Select(type => Instantiate(MainGameManager.Instance.GetPlantByType(type),
                         GridManager.Instance.transform, true).GetComponent<Plant>()).Select(plant => plant.ToField()))
            {
                _mouseEntity.Add(onFieldPlant.SetMouseIconMode());
            }

            // for (int i = 0; i < plantTypes.Count; i++)
            // {
            //     var plant = Instantiate(MainGameManager.Instance.GetPlantByType(plantTypes[i]),
            //         GridManager.Instance.transform, true).GetComponent<Plant>();
            //     var onFieldPlant = plant.ToField();
            //     _mouseEntity.Add(onFieldPlant.SetMouseIconMode());
            // }
        }
        
        public void CreateMousePlant(GameConfigObject.PlantType plantTypes, int index)
        {
            var plant = Instantiate(MainGameManager.Instance.GetPlantByType(plantTypes), GridManager.Instance.transform, true)
                .GetComponent<Plant>();
            var onFieldPlant = plant.ToField();
            if (onFieldPlant == null) return;
            _mouseEntity[index] = onFieldPlant.SetMouseIconMode();
        }

        private OnFieldEntity GetCurrentChosenMouseEntity()
        {
            return IsChooseCard() ? _mouseEntity[GetCurrentChosenCardIndex()] : null;
        }
        
        private OnFieldEntity GetCurrentChosenGridEntity()
        {
            return IsChooseCard() ? _gridEntity[GetCurrentChosenCardIndex()] : null;
        }

        private void DoUpdateMousePlantPosition(Vector3 position)
        {
            GetCurrentChosenMouseEntity().SetPosition(position);
        }
        
        private void UpdateMousePlantPosition()
        {
            var position = MainGameManager.Instance.GetNowMouseScreenToWorldPoint(10);
            GetCurrentChosenMouseEntity().SetPosition(position);
            // if (IsChooseCard())
            // {
            //     DoUpdateMousePlantPosition(position);
            // }
            
        }

        private void UpdateGridPlantPosition()
        {
            var position = GridManager.Instance.GetGridByPoint(_currentChosenPoint).Position;
            GetCurrentChosenGridEntity().SetLocalPosition(position);
        }

        private void DoResetMousePlantPosition(Vector3 position)
        {
            foreach (var onFieldEntity in _mouseEntity)
            {
                onFieldEntity.SetLocalPosition(position);
            }
        }
    
        public void ResetMousePlantPosition()
        {
            var position = new Vector3(1000f, 1000f, 1000f);
            foreach (var onFieldEntity in _mouseEntity)
            {
                onFieldEntity.SetLocalPosition(position);
            }
            // if (!IsChooseCard())
            // {
            //     DoResetMousePlantPosition(position);
            // }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == 0)
            {
                ChoosePlantCard();
            }
            else
            {
                CancelChoosePlantCard();
            }
        }

        private void DoSetCurrentChosenPoint(Vector2Int point)
        {
            _currentChosenPoint = point;
        }

        public void SetCurrentChosenPoint(GridManager.Grid grid)
        {
            if (grid == null) DoSetCurrentChosenPoint(GridManager.Grid.None.Point);
            else if (IsChooseCard() && !grid.IsOccupied())
            {
                DoSetCurrentChosenPoint(grid.Point);
            }
            else
            {
                DoSetCurrentChosenPoint(GridManager.Grid.None.Point);
            }
        }
        
        private bool IsCurrentChosenPointInGrid()
        {
            var currentChosenPoint = _currentChosenPoint;
            return GridManager.Grid.None.Point != currentChosenPoint;
        }
        
        private void DoPlacePlant(GridManager.Grid grid, GameConfigObject.PlantType plantType)
        {
            SoundManager.Instance.PlayEffect(GameSound.SoundType.Plante);
            SetCurrentChosenPoint(GridManager.Grid.None);
            
            var onFieldPlant = Instantiate(MainGameManager.Instance.GetPlantByType(plantType),
                _plantTransform, true).GetComponent<Plant>().ToField();

            grid.SetOnFieldCharacter(onFieldPlant.SetPlaceMode(grid) as OnFieldCharacter);
            
            // RegisterPlant(onFieldPlant);
        }

        private bool PlacePlant(GridManager.Grid grid, GameConfigObject.PlantType plantType)
        {
            if (grid.IsOccupied()) return false;
            DoPlacePlant(grid, plantType);
            return true;
        }

        private void DoAfterPlacePlant(int price)
        {
            SunManager.Instance.SubCurrentSunLight(price);
            SeedCardManager.Instance.AfterPlacePlant(_currentChosenCardIndex);
            SetCurrentChosenCardIndex(-1);
            MainGameManager.Instance.SetMouseStatus(MainGameManager.MouseEvent.None);
        }

        public bool AfterPlacePlant(int price)
        {
            if (!IsChooseCard()) return false;
            DoAfterPlacePlant(price);
            return true;
        }
        
        public void PlaceChosenPlant()
        {
            if(!IsChooseCard() || !IsCurrentChosenPointInGrid()) return;
            
            var price = SeedCardManager.Instance.GetCard(_currentChosenCardIndex).GetSunPrice();
            if(SunManager.Instance.GetCurrentSunLight() < price) return;
            
            var grid = GridManager.Instance.GetGridByPoint(_currentChosenPoint);
            var type = SeedCardManager.Instance.GetPlantType(_currentChosenCardIndex);
            if (grid.IsOccupied()) return;
            
            DoPlacePlant(grid, type);
            DoAfterPlacePlant(price);
        }
        
        private void Awake()
        {
            Instance = this;
            _plantTransform = GameObject.Find("/UI/Grid/Plant").transform;
        }


        private void Start()
        {
            SetCurrentChosenCardIndex(-1);
            SetCurrentPutOnCardIndex(-1);
            SetCurrentChosenPoint(GridManager.Grid.None);
        }

        private void Update()
        {
            // Debug.Log(IsChooseCard() && IsCurrentChosenPointInGrid());
            // Debug.Log(_currentChosenPoint);
            if (IsChooseCard())
            {
                UpdateMousePlantPosition();
            }
            else
            {
                ResetMousePlantPosition();
            }
            
            if (IsChooseCard() && IsCurrentChosenPointInGrid())
            {
                UpdateGridPlantPosition();
            }
            else
            {
                ResetGridPlantPosition();
            }
        }
    }
}