using System;
using PvZ.Core.Entities;
using PvZ.Gameplay.Plants;
using PvZ.Presentation;
using UnityEngine;

namespace PvZ.Gameplay.Board
{
    /// <summary>
    /// Owns the two temporary visuals used during one planting session.
    /// </summary>
    internal sealed class PlantingPreview : IDisposable
    {
        private GameEntity _cursorPreview;
        private GameEntity _gridPreview;

        public PlantingPreview(PlantDefinition definition, Transform previewParent)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (previewParent == null) throw new ArgumentNullException(nameof(previewParent));

            var plantPrefab = definition.PresentationPrefab;
            if (plantPrefab == null) throw new MissingReferenceException("Plant definition requires a presentation prefab.");

            try
            {
                _cursorPreview = EntityPresentation.ConfigureCursorPreview(
                    CreatePlant(plantPrefab, previewParent),
                    definition.CardIconFrame);
                _gridPreview = EntityPresentation.ConfigureGridPreview(
                    CreatePlant(plantPrefab, previewParent),
                    definition.CardIconFrame);
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
            if (_cursorPreview is PlantEntity plant)
            {
                plant.AlignShadowAnchorToWorldPosition(worldPosition);
            }
        }

        public void ShowGrid(GridManager.Grid grid)
        {
            if (_gridPreview == null || grid == null) return;

            _gridPreview.SetSortingLayer("plant-" + grid.Point.y);
            _gridPreview.SetLocalPosition(grid.Position);
            if (_gridPreview is PlantEntity plant)
            {
                plant.AlignShadowAnchorToParentPosition(grid.Position);
            }
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

        private static PlantEntity CreatePlant(GameObject prefab, Transform parent)
        {
            var instance = UnityEngine.Object.Instantiate(prefab, parent, false);
            var plant = instance.GetComponent<PlantEntity>();
            if (plant == null)
            {
                UnityEngine.Object.Destroy(instance);
                throw new MissingComponentException($"{prefab.name} requires a Plant component");
            }

            return plant;
        }

        private static void DestroyPreview(ref GameEntity preview)
        {
            if (preview != null) UnityEngine.Object.Destroy(preview.gameObject);
            preview = null;
        }
    }
}
