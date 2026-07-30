using UnityEngine;

namespace Script.Model
{
    using Prefab.Object.Shadow.Script;
    using Script.Manager;
    using Script.Util;

    public abstract class Plant : Character
    {
        [SerializeField] private Transform shadowTransform;
        private GridManager.Grid _occupiedGrid;
        private Shadow _shadow;

        public Plant PlaceOn(GridManager.Grid grid)
        {
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
            return this;
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

            _shadow.SetSize(0.7f).ToField();
        }

        protected virtual void OnDestroy()
        {
            _occupiedGrid?.TryRelease(this);
            _occupiedGrid = null;
        }
        
        public void SetNormalMode()
        {
            SetSortingLayer("plant-"+Row);
        }
    }
}
