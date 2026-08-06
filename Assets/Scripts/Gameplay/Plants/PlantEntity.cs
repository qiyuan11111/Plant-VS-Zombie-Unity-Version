using UnityEngine;
using PvZ.Bootstrap;
using PvZ.Core.Entities;
using PvZ.Gameplay.Board;
using PvZ.Gameplay.World;

namespace PvZ.Gameplay.Plants
{
    using PvZ.Config;

    public abstract class PlantEntity : Character
    {
        private static readonly Vector3 BoardScale = Vector3.one;

        [SerializeField] private ShadowSizePreset shadowSize = ShadowSizePreset.Large;
        [SerializeField] private Vector2 shadowImageTopLeft = new(-3f, 51f);
        [SerializeField] private bool drawsShadow = true;
        private GridManager.Grid _occupiedGrid;
        private Shadow _shadow;
        private bool _isOnBoard;

        public bool IsOnBoard => _isOnBoard;
        public ShadowSizePreset ShadowSize => shadowSize;
        public Vector2 ShadowImageTopLeft => shadowImageTopLeft;
        public Vector3 ShadowCenterLocalPosition => Shadow.SourceTopLeftToUnityCenter(shadowImageTopLeft);
        public bool DrawsShadow => drawsShadow;

        public PlantEntity EnterBoard(GridManager.Grid grid)
        {
            if (_isOnBoard)
            {
                throw new System.InvalidOperationException($"{name} is already on the board.");
            }

            if (grid == null || grid == GridManager.Grid.None)
            {
                throw new System.ArgumentException("A plant requires a valid grid.", nameof(grid));
            }

            _occupiedGrid = grid;
            SetRow(grid.Point.y).SetHeight(0f);

            GetComponentRoot()
                .SetSortingLayer("plant-" + grid.Point.y)
                .SetColliderState(true);

            SetLocalScale(BoardScale);
            SetLocalPosition(new Vector3(grid.LogicalOrigin.x, grid.LogicalOrigin.y, 10f));
            SetName(GetEnglishName() + "-" + grid.Point.x + "-" + grid.Point.y);

            EnsureShadow();
            _isOnBoard = true;
            enabled = true;
            OnEnteredBoard();
            return this;
        }

        protected virtual void OnEnteredBoard()
        {
        }

        private void EnsureShadow()
        {
            if (_shadow != null || !drawsShadow) return;
            var shadowPrefab = MainGameManager.Instance.GetObjectByType(GameConfigObject.ObjectType.PlanteShadow);
            var shadowObject = Instantiate(shadowPrefab, Transform, false);
            shadowObject.transform.localPosition = ShadowCenterLocalPosition;
            shadowObject.transform.localRotation = Quaternion.identity;
            _shadow = shadowObject.GetComponent<Shadow>();

            if (_shadow == null)
            {
                Destroy(shadowObject);
                throw new MissingComponentException($"{shadowPrefab.name} requires a Shadow component.");
            }

            var drawNightShadow = GridManager.Instance != null && GridManager.Instance.IsNight;
            _shadow.Initialize(shadowSize, drawNightShadow);
        }

        protected virtual void OnDestroy()
        {
            _isOnBoard = false;
            _occupiedGrid?.TryRelease(this);
            _occupiedGrid = null;
        }
        
        public void SetNormalMode()
        {
            SetSortingLayer("plant-"+Row);
        }
    }
}
