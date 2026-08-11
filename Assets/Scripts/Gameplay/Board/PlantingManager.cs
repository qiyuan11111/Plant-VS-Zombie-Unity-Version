using PvZ.Audio;
using PvZ.Bootstrap;
using PvZ.Core;
using PvZ.Gameplay.SeedCards;
using PvZ.Gameplay.Plants;
using PvZ.Gameplay.Sun;
using UnityEngine;

namespace PvZ.Gameplay.Board
{
    /// <summary>
    /// Coordinates one planting transaction from card selection to placement or cancellation.
    /// </summary>
    public class PlantingManager : SceneSingleton<PlantingManager>
    {
        public enum PlantingState
        {
            Idle,
            Holding
        }

        [SerializeField] private Transform plantParent;

        private PlantingState _state = PlantingState.Idle;
        private SeedCard _selectedCard;
        private GridManager.Grid _hoveredGrid;
        private PlantingPreview _preview;
        private bool _isConfigured;

        public bool IsPlanting => _state == PlantingState.Holding;

        public bool IsSelected(SeedCard card)
        {
            return IsPlanting && _selectedCard == card;
        }

        public bool TryBegin(SeedCard card)
        {
            if (!_isConfigured || card == null) return false;

            // Preserve the previous interaction: clicking a card while holding a plant cancels it.
            if (IsPlanting)
            {
                Cancel();
                return false;
            }

            if (!card.CanPlant) return false;

            var prefab = card.Definition.Prefab;
            if (prefab == null) return false;

            // Build the preview before committing the planting session. The
            // constructor validates and instantiates presentation prefabs, so it
            // can throw without leaving this manager stuck in Holding state.
            var preview = new PlantingPreview(card.Definition, GridManager.Instance.transform);

            _selectedCard = card;
            _state = PlantingState.Holding;
            _preview = preview;

            card.OnChoose();
            SoundManager.Instance.PlayEffect(GameSound.SoundType.SeedLift);
            UpdatePreview();
            return true;
        }

        public void Cancel()
        {
            if (!IsPlanting) return;

            var card = _selectedCard;
            EndSession();
            card?.CancelChoose();
        }

        public bool TryPlace()
        {
            if (!_isConfigured || !IsPlanting || _selectedCard == null || !IsValidGrid(_hoveredGrid))
                return false;

            var price = _selectedCard.SunPrice;
            if (SunManager.Instance.GetCurrentSunLight() < price) return false;

            var prefab = _selectedCard.Definition.Prefab;
            if (prefab == null) return false;

            var plantObject = Instantiate(prefab, plantParent, false);
            var plant = plantObject.GetComponent<PlantEntity>();
            if (plant == null)
            {
                Destroy(plantObject);
                return false;
            }

            if (!_hoveredGrid.TryOccupy(plant))
            {
                Destroy(plantObject);
                return false;
            }

            try
            {
                plant.EnterBoard(_hoveredGrid);
            }
            catch (System.Exception exception)
            {
                _hoveredGrid.TryRelease(plant);
                Destroy(plantObject);
                Debug.LogException(exception, plantObject);
                return false;
            }

            var card = _selectedCard;
            EndSession();

            SoundManager.Instance.PlayEffect(GameSound.SoundType.Plante);
            card.AfterPlace();
            SunManager.Instance.SubCurrentSunLight(price);
            return true;
        }

        private void UpdatePreview()
        {
            if (!IsPlanting || _preview == null) return;

            var cursorPosition = MainGameManager.Instance.GetNowMouseScreenToWorldPoint(10f);
            _preview.SetCursorPosition(cursorPosition);

            var grid = GridManager.Instance.GetGridByWorldPosition(cursorPosition);
            if (IsValidGrid(grid))
            {
                _hoveredGrid = grid;
                _preview.ShowGrid(grid);
            }
            else
            {
                _hoveredGrid = null;
                _preview.HideGrid();
            }
        }

        private static bool IsValidGrid(GridManager.Grid grid)
        {
            return grid != null && grid != GridManager.Grid.None && !grid.IsOccupied();
        }

        private void EndSession()
        {
            _state = PlantingState.Idle;
            _selectedCard = null;
            _hoveredGrid = null;

            _preview?.Dispose();
            _preview = null;
        }

        protected override bool ValidateReferences()
        {
            return RequireReference(plantParent, nameof(plantParent));
        }

        protected override void OnSingletonStart()
        {
            _isConfigured = true;
        }

        protected override bool ValidateDependencies()
        {
            var isValid = true;
            isValid &= RequireManager(MainGameManager.Instance);
            isValid &= RequireManager(GridManager.Instance);
            isValid &= RequireManager(SunManager.Instance);
            isValid &= RequireManager(SoundManager.Instance);
            return isValid;
        }

        private void Update()
        {
            if (!IsPlanting) return;

            if (UnityEngine.Input.GetMouseButtonDown(1) || UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                Cancel();
                return;
            }

            UpdatePreview();
        }

        protected override void OnSingletonDestroy()
        {
            _preview?.Dispose();
        }
    }
}
