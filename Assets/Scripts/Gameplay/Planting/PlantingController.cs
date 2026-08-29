using PvZ.Audio;
using PvZ.Bootstrap;
using PvZ.Core;
using PvZ.Gameplay.Board;
using PvZ.Gameplay.Planting.Presentation;
using PvZ.Gameplay.SeedCards;
using PvZ.Gameplay.Plants;
using PvZ.Gameplay.Sun;
using PvZ.Input;
using UnityEngine;

namespace PvZ.Gameplay.Planting
{
    /// <summary>
    /// Coordinates one planting transaction from card selection to placement or cancellation.
    /// </summary>
    public sealed class PlantingController : SceneSingleton<PlantingController>
    {
        private enum State
        {
            Idle,
            Holding
        }

        [SerializeField] private Transform plantParent;

        private State _state = State.Idle;
        private SeedCard _selectedCard;
        private BoardCell _hoveredCell;
        private PlantingPreview _preview;
        private bool _isConfigured;

        public bool IsPlanting => _state == State.Holding;

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
            var preview = new PlantingPreview(card.Definition, BoardGrid.Instance.transform);

            _selectedCard = card;
            _state = State.Holding;
            _preview = preview;

            card.OnChoose();
            SoundManager.Instance.PlayEffect(SoundCue.SeedLift);
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
            if (!_isConfigured || !IsPlanting || _selectedCard == null || !IsValidCell(_hoveredCell))
                return false;

            var price = _selectedCard.SunPrice;
            if (!SunWallet.Instance.CanAfford(price)) return false;

            var prefab = _selectedCard.Definition.Prefab;
            if (prefab == null) return false;

            var plantObject = Instantiate(prefab, plantParent, false);
            var plant = plantObject.GetComponent<PlantEntity>();
            if (plant == null)
            {
                Destroy(plantObject);
                return false;
            }

            if (!_hoveredCell.TryOccupy(plant))
            {
                Destroy(plantObject);
                return false;
            }

            try
            {
                plant.EnterBoard(_hoveredCell);
            }
            catch (System.Exception exception)
            {
                _hoveredCell.TryRelease(plant);
                Destroy(plantObject);
                Debug.LogException(exception, plantObject);
                return false;
            }

            var card = _selectedCard;
            EndSession();

            SoundManager.Instance.PlayEffect(SoundCue.Plant);
            card.AfterPlace();
            SunWallet.Instance.TrySpend(price);
            return true;
        }

        private void UpdatePreview()
        {
            if (!IsPlanting || _preview == null) return;

            var cursorPosition = PointerWorldPosition.Get(10f);
            _preview.SetCursorPosition(cursorPosition);

            var cell = BoardGrid.Instance.GetCellAtWorldPosition(cursorPosition);
            if (IsValidCell(cell))
            {
                _hoveredCell = cell;
                _preview.ShowCell(cell);
            }
            else
            {
                _hoveredCell = null;
                _preview.HideCell();
            }
        }

        private static bool IsValidCell(BoardCell cell)
        {
            return cell != null && cell != BoardCell.None && !cell.IsOccupied;
        }

        private void EndSession()
        {
            _state = State.Idle;
            _selectedCard = null;
            _hoveredCell = null;

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
            isValid &= RequireManager(GameBootstrap.Instance);
            isValid &= RequireManager(BoardGrid.Instance);
            isValid &= RequireManager(SunWallet.Instance);
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
