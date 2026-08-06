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
        private const string ShadowAnchorName = "Shadow_Anchor";
        private const float LegacyGroundOffsetY = -17f;
        private static readonly Vector3 BoardScale = new(1.025f, 1.025f, 1f);

        [SerializeField] private Transform shadowTransform;
        [SerializeField] private ShadowSizePreset shadowSize = ShadowSizePreset.Large;
        private GridManager.Grid _occupiedGrid;
        private Shadow _shadow;
        private bool _isOnBoard;

        public bool IsOnBoard => _isOnBoard;
        public Transform ShadowAnchor => ResolveShadowAnchor();
        public ShadowSizePreset ShadowSize => shadowSize;

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

            var gridCenterPosition = new Vector3(grid.Position.x, grid.Position.y, 10f);
            var groundPosition = GetBoardGroundPosition(grid.Position, gridCenterPosition.z);
            SetLocalScale(BoardScale);
            SetLocalPosition(gridCenterPosition);
            AlignShadowAnchorToParentPosition(groundPosition);
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

        /// <summary>
        /// Grid coordinates were authored as plant artwork centers. The shared
        /// ground line therefore stays one legacy foot offset below each center,
        /// while individual artwork is corrected through its shadow anchor.
        /// </summary>
        public static Vector3 GetBoardGroundPosition(Vector2 gridCenter, float z = 0f)
        {
            return new Vector3(
                gridCenter.x,
                gridCenter.y + LegacyGroundOffsetY * BoardScale.y,
                z);
        }

        /// <summary>
        /// Moves the plant root so its shadow anchor lands on a position expressed
        /// in the plant parent's coordinate space. The plant artwork is deliberately
        /// not centered; every presentation context aligns the same ground point.
        /// </summary>
        public PlantEntity AlignShadowAnchorToParentPosition(Vector3 anchorPosition)
        {
            var anchor = RequireShadowAnchor();
            var root = transform;
            var parent = root.parent;
            var currentAnchorPosition = parent != null
                ? parent.InverseTransformPoint(anchor.position)
                : anchor.position;

            root.localPosition += anchorPosition - currentAnchorPosition;
            return this;
        }

        public PlantEntity AlignShadowAnchorToWorldPosition(Vector3 anchorPosition)
        {
            var parent = transform.parent;
            return AlignShadowAnchorToParentPosition(parent != null
                ? parent.InverseTransformPoint(anchorPosition)
                : anchorPosition);
        }

        private void EnsureShadow()
        {
            if (_shadow != null) return;
            var shadowPosition = RequireShadowAnchor().position;
            var shadowPrefab = MainGameManager.Instance.GetObjectByType(GameConfigObject.ObjectType.PlanteShadow);
            var shadowObject = Instantiate(shadowPrefab, shadowPosition, Quaternion.identity, Transform);
            _shadow = shadowObject.GetComponent<Shadow>();

            if (_shadow == null)
            {
                Destroy(shadowObject);
                throw new MissingComponentException($"{shadowPrefab.name} requires a Shadow component.");
            }

            _shadow.Initialize(shadowSize);
        }

        private Transform RequireShadowAnchor()
        {
            var anchor = ResolveShadowAnchor();
            if (anchor != null) return anchor;

            throw new MissingReferenceException(
                $"{name} requires a child transform named {ShadowAnchorName}.");
        }

        private Transform ResolveShadowAnchor()
        {
            if (shadowTransform != null) return shadowTransform;

            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name != ShadowAnchorName) continue;

                shadowTransform = child;
                break;
            }

            return shadowTransform;
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
