using System;
using PvZ.Config;
using PvZ.Gameplay.Entities;
using PvZ.Gameplay.Board;
using PvZ.Gameplay.Plants;
using PvZ.Presentation.Rendering;
using PvZ.Gameplay.Presentation.EntityPreviews;
using UnityEngine;

namespace PvZ.Gameplay.Planting.Presentation
{
    /// <summary>
    /// Owns the two temporary visuals used during one planting session.
    /// </summary>
    internal sealed class PlantingPreview : IDisposable
    {
        private GameEntity _cursorPreview;
        private GameEntity _cellPreview;

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
                    definition.PresentationNormalizedTime);
                _cellPreview = EntityPresentation.ConfigureCellPreview(
                    CreatePlant(plantPrefab, previewParent),
                    definition.PresentationNormalizedTime);
                _cellPreview.gameObject.SetActive(false);
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
                plant.SetLocalPosition(GetLocalCursorPosition(plant.transform.parent, worldPosition));
            }
        }

        internal static Vector3 GetLocalCursorPosition(Transform previewParent, Vector3 pointerWorldPosition)
        {
            if (previewParent == null) throw new ArgumentNullException(nameof(previewParent));

            return previewParent.InverseTransformPoint(pointerWorldPosition);
        }

        public void ShowCell(BoardCell cell)
        {
            if (_cellPreview == null || cell == null) return;

            _cellPreview.SetSortingLayer("plant-" + cell.Point.y);
            _cellPreview.SetLocalPosition(cell.Position);
            _cellPreview.gameObject.SetActive(true);
        }

        public void HideCell()
        {
            if (_cellPreview != null) _cellPreview.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            DestroyPreview(ref _cursorPreview);
            DestroyPreview(ref _cellPreview);
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
