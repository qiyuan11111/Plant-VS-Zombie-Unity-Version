using Prefab.Object.SeedCard.Script;
using Script.Model;
using UnityEngine;

namespace Script.Manager
{
    /// <summary>
    /// Coordinates one planting transaction from card selection to placement or cancellation.
    /// </summary>
    public class PlantingManager : MonoBehaviour
    {
        public enum PlantingState
        {
            Idle,
            Holding
        }

        public static PlantingManager Instance;

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

            _selectedCard = card;
            _state = PlantingState.Holding;
            _preview = new PlantingPreview(prefab, GridManager.Instance.transform);

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
            var plant = plantObject.GetComponent<Plant>();
            if (plant == null)
            {
                Destroy(plantObject);
                return false;
            }

            var entity = plant.ToField();
            var character = entity as Character;
            if (character == null)
            {
                Destroy(plantObject);
                return false;
            }

            character.SetPlaceMode(_hoveredGrid);
            if (!_hoveredGrid.TrySetCharacter(character))
            {
                Destroy(plantObject);
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

        private void Awake()
        {
            Instance = this;
            _isConfigured = ValidateReferences();
            enabled = _isConfigured;
        }

        private bool ValidateReferences()
        {
            if (plantParent != null) return true;

            Debug.LogError(
                $"{nameof(PlantingManager)} requires {nameof(plantParent)} to be assigned in the Inspector.",
                this);
            return false;
        }

        private void Update()
        {
            if (!IsPlanting) return;

            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                Cancel();
                return;
            }

            UpdatePreview();
        }

        private void OnDestroy()
        {
            _preview?.Dispose();
            if (Instance == this) Instance = null;
        }
    }
}
