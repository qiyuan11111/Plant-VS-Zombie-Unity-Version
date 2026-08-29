using System.Collections.Generic;
using PvZ.Core;
using UnityEngine;

namespace PvZ.Gameplay.Board
{
    /// <summary>Builds and queries the logical board-cell map.</summary>
    public sealed class BoardGrid : SceneSingleton<BoardGrid>
    {
        [SerializeField] private BoardTerrain terrain = BoardTerrain.FrontYard;
        [SerializeField] private bool isNight;
        [SerializeField] private List<Vector2Int> highGroundCells = new();

        private int _columnCount;
        private int _rowCount;
        private BoardCell[,] _cells;

        public BoardTerrain Terrain => terrain;
        public bool IsNight => isNight;

        public BoardCell GetCell(Vector2Int point)
        {
            return point.x >= 0 && point.x < _columnCount &&
                   point.y >= 0 && point.y < _rowCount
                ? _cells[point.x, point.y]
                : null;
        }

        public BoardCell GetCellAtWorldPosition(Vector3 worldPosition)
        {
            Vector2 localPosition = transform.InverseTransformPoint(worldPosition);
            return BoardGeometry.TryLocalPositionToCell(terrain, localPosition, out var point)
                ? GetCell(point)
                : BoardCell.None;
        }

        protected override void OnReferencesValidated()
        {
            _columnCount = BoardGeometry.ColumnCount;
            _rowCount = BoardGeometry.GetRowCount(terrain);
            _cells = new BoardCell[_columnCount, _rowCount];

            for (var column = 0; column < _columnCount; column++)
            {
                for (var row = 0; row < _rowCount; row++)
                {
                    var point = new Vector2Int(column, row);
                    var geometry = BoardGeometry.GetCell(
                        terrain,
                        column,
                        row,
                        highGroundCells.Contains(point));
                    _cells[column, row] = new BoardCell(point, geometry);
                }
            }
        }
    }
}
