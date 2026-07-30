using System;
using Script.Model;
using UnityEngine;

namespace Script.Manager
{
    /// <summary>
    /// Owns the two temporary visuals used during one planting session.
    /// </summary>
    internal sealed class PlantingPreview : IDisposable
    {
        private Entity _cursorPreview;
        private Entity _gridPreview;

        public PlantingPreview(GameObject plantPrefab, Transform previewParent)
        {
            if (plantPrefab == null) throw new ArgumentNullException(nameof(plantPrefab));
            if (previewParent == null) throw new ArgumentNullException(nameof(previewParent));

            try
            {
                _cursorPreview = CreatePlant(plantPrefab, previewParent).SetMouseIconMode();
                _gridPreview = CreatePlant(plantPrefab, previewParent).SetGridIconMode();
                _gridPreview.gameObject.SetActive(false);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void SetCursorPosition(Vector3 worldPosition)
        {
            if (_cursorPreview != null) _cursorPreview.SetPosition(worldPosition);
        }

        public void ShowGrid(GridManager.Grid grid)
        {
            if (_gridPreview == null || grid == null) return;

            _gridPreview.SetSortingLayer("plant-" + grid.Point.y);
            _gridPreview.SetLocalPosition(grid.Position);
            _gridPreview.gameObject.SetActive(true);
        }

        public void HideGrid()
        {
            if (_gridPreview != null) _gridPreview.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            DestroyPreview(ref _cursorPreview);
            DestroyPreview(ref _gridPreview);
        }

        private static Entity CreatePlant(GameObject prefab, Transform parent)
        {
            var instance = UnityEngine.Object.Instantiate(prefab, parent, false);
            var plant = instance.GetComponent<Plant>();
            if (plant == null)
            {
                UnityEngine.Object.Destroy(instance);
                throw new MissingComponentException($"{prefab.name} requires a Plant component");
            }

            var entity = plant.ToField();
            if (entity != null) return entity;

            UnityEngine.Object.Destroy(instance);
            throw new InvalidOperationException($"{prefab.name}.ToField returned null");
        }

        private static void DestroyPreview(ref Entity preview)
        {
            if (preview != null) UnityEngine.Object.Destroy(preview.gameObject);
            preview = null;
        }
    }
}
