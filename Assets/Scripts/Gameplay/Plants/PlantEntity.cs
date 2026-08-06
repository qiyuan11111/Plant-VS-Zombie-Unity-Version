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
        private static readonly Vector3 BoardScale = Vector3.one;

        [SerializeField] private Transform shadowTransform;
        [SerializeField] private ShadowSizePreset shadowSize = ShadowSizePreset.Large;
        [SerializeField] private Vector2 shadowOffset = new(0f, 5f);
        [SerializeField] private bool drawsShadow = true;
        private GridManager.Grid _occupiedGrid;
        private Shadow _shadow;
        private bool _isOnBoard;

        public bool IsOnBoard => _isOnBoard;
        public Transform ShadowAnchor => ResolveShadowAnchor();
        public Transform GroundAnchor => ResolveShadowAnchor();
        public ShadowSizePreset ShadowSize => shadowSize;
        public Vector2 ShadowOffset => shadowOffset;
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

            var gridCenterPosition = new Vector3(grid.Position.x, grid.Position.y, 10f);
            var groundPosition = new Vector3(grid.GroundPosition.x, grid.GroundPosition.y, gridCenterPosition.z);
            SetLocalScale(BoardScale);
            SetLocalPosition(gridCenterPosition);
            AlignGroundAnchorToParentPosition(groundPosition);
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
        /// Converts a normal-lawn cell center to the original plant ground point.
        /// New code should prefer Grid.GroundPosition so pool and roof geometry is retained.
        /// </summary>
        public static Vector3 GetBoardGroundPosition(Vector2 gridCenter, float z = 0f)
        {
            return new Vector3(
                gridCenter.x,
                gridCenter.y - 24f,
                z);
        }

        /// <summary>
        /// Converts the original plant-local ground point into a prefab-local
        /// anchor. Presentation prefabs normalize their FLA coordinates around
        /// the basic SpriteTransform's static spritePosition, not around the
        /// original Reanimation draw origin.
        /// </summary>
        public static Vector3 GetGroundAnchorLocalPosition(Vector2 basicSpritePosition)
        {
            return new Vector3(
                BoardGeometry.PlantGroundSourcePosition.x - basicSpritePosition.x,
                basicSpritePosition.y - BoardGeometry.PlantGroundSourcePosition.y,
                0f);
        }

        /// <summary>
        /// Moves the plant root so its shadow anchor lands on a position expressed
        /// in the plant parent's coordinate space. The plant artwork is deliberately
        /// not centered; every presentation context aligns the same ground point.
        /// </summary>
        public PlantEntity AlignGroundAnchorToParentPosition(Vector3 anchorPosition)
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

        public PlantEntity AlignGroundAnchorToWorldPosition(Vector3 anchorPosition)
        {
            var parent = transform.parent;
            return AlignGroundAnchorToParentPosition(parent != null
                ? parent.InverseTransformPoint(anchorPosition)
                : anchorPosition);
        }

        public PlantEntity AlignShadowAnchorToParentPosition(Vector3 anchorPosition)
        {
            return AlignGroundAnchorToParentPosition(anchorPosition);
        }

        public PlantEntity AlignShadowAnchorToWorldPosition(Vector3 anchorPosition)
        {
            return AlignGroundAnchorToWorldPosition(anchorPosition);
        }

        private void EnsureShadow()
        {
            if (_shadow != null || !drawsShadow) return;
            var anchorPosition = RequireShadowAnchor().position;
            var shadowPosition = anchorPosition + new Vector3(shadowOffset.x, shadowOffset.y, 0f);
            var shadowPrefab = MainGameManager.Instance.GetObjectByType(GameConfigObject.ObjectType.PlanteShadow);
            var shadowObject = Instantiate(shadowPrefab, shadowPosition, Quaternion.identity, Transform);
            _shadow = shadowObject.GetComponent<Shadow>();

            if (_shadow == null)
            {
                Destroy(shadowObject);
                throw new MissingComponentException($"{shadowPrefab.name} requires a Shadow component.");
            }

            var drawNightShadow = GridManager.Instance != null && GridManager.Instance.IsNight;
            _shadow.Initialize(shadowSize, drawNightShadow);
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
