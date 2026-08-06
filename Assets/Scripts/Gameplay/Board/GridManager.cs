using System.Collections.Generic;
using PvZ.Core;
using PvZ.Core.Entities;
using PvZ.Gameplay.Plants;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PvZ.Gameplay.Board
{
    public class GridManager : SceneSingleton<GridManager>, IPointerClickHandler
    {
        [SerializeField] private BoardTerrain terrain = BoardTerrain.FrontYard;
        [SerializeField] private bool isNight;
        [SerializeField] private List<Vector2Int> highGroundCells = new();

        private int _rowNum, _colNum;

        private Grid[,] _gridMap;

        public BoardTerrain Terrain => terrain;
        public bool IsNight => isNight;

        private void CreateGrids()
        {
            for (var column = 0; column < _rowNum; column++)
            {
                for (var row = 0; row < _colNum; row++)
                {
                    var point = new Vector2Int(column, row);
                    var geometry = BoardGeometry.GetCell(
                        terrain,
                        column,
                        row,
                        highGroundCells.Contains(point));
                    _gridMap[column, row] = new Grid(point, geometry);
                }
            }
        }

        public Grid GetGridByPoint(Vector2Int point)
        {
            if (point.x >= 0 && point.x < _rowNum && point.y >= 0 && point.y < _colNum)
                return _gridMap[point.x, point.y];
            return null;
        }

        public Grid GetGridByWorldPosition(Vector3 worldPosition)
        {
            Vector2 localPosition = transform.InverseTransformPoint(worldPosition);
            return BoardGeometry.TryLocalPositionToCell(terrain, localPosition, out var point)
                ? GetGridByPoint(point)
                : Grid.None;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == 0)
            {
                PlantingManager.Instance.TryPlace();
            }
            else
            {
                PlantingManager.Instance.Cancel();
            }
        }

        protected override void OnReferencesValidated()
        {
            _rowNum = BoardGeometry.ColumnCount;
            _colNum = BoardGeometry.GetRowCount(terrain);
            _gridMap = new Grid[_rowNum, _colNum];

            CreateGrids();
        }

        protected override bool ValidateDependencies()
        {
            return RequireManager(PlantingManager.Instance);
        }


        public class Grid
        {
            /// <summary>
            ///  网格的位置(0,1)
            /// </summary>
            public Vector2Int Point;

            /// <summary>
            ///  网格的坐标(0,1)
            /// </summary>
            public Vector2 Position;
            public Vector2 LogicalOrigin;
            public Vector2 GroundPosition;
            public Vector2 Size;

            private PlantEntity _plant;
            private readonly bool _acceptsOccupant;

            public PlantEntity Occupant => _plant;

            public bool IsOccupied()
            {
                return _plant != null;
            }

            public bool TryOccupy(PlantEntity plant)
            {
                if (!_acceptsOccupant || IsOccupied() || plant == null) return false;

                _plant = plant;

                return true;
            }

            public bool TryRelease(PlantEntity plant)
            {
                if (_plant != plant) return false;

                _plant = null;
                return true;
            }

            public static readonly Grid None = new(
                new Vector2Int(-1, -1),
                new Vector2(-1, -1),
                false);

            public Grid(Vector2Int point, Vector2 position)
                : this(point, position, true)
            {
            }

            public Grid(Vector2Int point, BoardCellGeometry geometry)
                : this(point, geometry.Center, true)
            {
                LogicalOrigin = geometry.LogicalOrigin;
                GroundPosition = geometry.Ground;
                Size = geometry.Size;
            }

            private Grid(Vector2Int point, Vector2 position, bool acceptsOccupant)
            {
                Point = point;
                Position = position;
                LogicalOrigin = position + new Vector2(-40f, 50f);
                GroundPosition = position + new Vector2(0f, -24f);
                Size = new Vector2(80f, 100f);
                _acceptsOccupant = acceptsOccupant;
            }

            public bool Contains(Vector2 localPosition)
            {
                var delta = localPosition - Position;
                return Mathf.Abs(delta.x) < Size.x * 0.5f &&
                       Mathf.Abs(delta.y) < Size.y * 0.5f;
            }
        }
    }
}
