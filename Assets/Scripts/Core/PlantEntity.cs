using UnityEngine;

namespace Script.Model
{
    using Prefab.Object.Shadow.Script;
    using Script.Manager;
    using Script.Util;

    public abstract class PlantEntity : Character
    {
        [SerializeField] private Transform shadowTransform;
        private GridManager.Grid _occupiedGrid;
        private Shadow _shadow;
        private bool _isOnBoard;

        public bool IsOnBoard => _isOnBoard;

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

            SetLocalScale(new Vector3(1.025f, 1.025f, 1f))
                .SetLocalPosition(new Vector3(grid.Position.x, grid.Position.y, 10f))
                .SetName(GetEnglishName() + "-" + grid.Point.x + "-" + grid.Point.y);

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
            if (_shadow != null) return;
            if (shadowTransform == null) shadowTransform = Transform.Find("Shadow_Anchor");

            var shadowPosition = shadowTransform != null
                ? shadowTransform.position
                : Transform.TransformPoint(new Vector3(0f, -17f, 0f));
            var shadowPrefab = MainGameManager.Instance.GetObjectByType(GameConfigObject.ObjectType.PlanteShadow);
            var shadowObject = Instantiate(shadowPrefab, shadowPosition, Quaternion.identity, Transform);
            _shadow = shadowObject.GetComponent<Shadow>();

            if (_shadow == null)
            {
                Destroy(shadowObject);
                throw new MissingComponentException($"{shadowPrefab.name} requires a Shadow component.");
            }

            _shadow.Initialize(0.7f);
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
