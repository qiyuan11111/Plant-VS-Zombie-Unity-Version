using UnityEngine;

namespace PvZ.Gameplay.Board
{
    public enum BoardTerrain
    {
        FrontYard,
        Pool,
        Roof
    }

    public readonly struct BoardCellGeometry
    {
        public BoardCellGeometry(Vector2 logicalOrigin, Vector2 center, Vector2 ground, Vector2 size)
        {
            LogicalOrigin = logicalOrigin;
            Center = center;
            Ground = ground;
            Size = size;
        }

        public Vector2 LogicalOrigin { get; }
        public Vector2 Center { get; }
        public Vector2 Ground { get; }
        public Vector2 Size { get; }
    }

    /// <summary>
    /// Converts the original 800x600 board coordinates (Y down) to the Grid
    /// RectTransform's local coordinates (Y up). The scene authors the Grid pivot
    /// at original screen position (480,300). Values mirror Board::GridToPixelX/Y.
    /// </summary>
    public static class BoardGeometry
    {
        public const int ColumnCount = 9;
        public const int FrontYardRowCount = 5;
        public const int PoolRowCount = 6;
        public const float CellWidth = 80f;
        public const float FrontYardCellHeight = 100f;
        public const float PoolAndRoofCellHeight = 85f;
        public const float HighGroundHeight = 30f;

        private const float GridPivotScreenX = 480f;
        private const float GridPivotScreenY = 300f;
        private const float LawnMinX = 40f;
        private const float LawnMinY = 80f;
        private static readonly Vector2 FirstLogicalOrigin = new(-440f, 220f);
        private static readonly Vector2 PlantGroundOffset = new(40f, -74f);

        // CursorObject draws an ordinary plant so the pointer is plant-local (35, 60).
        public static readonly Vector2 CursorToGroundOffset = new(5f, -14f);

        public static int GetRowCount(BoardTerrain terrain)
        {
            return terrain == BoardTerrain.Pool ? PoolRowCount : FrontYardRowCount;
        }

        public static BoardCellGeometry GetCell(
            BoardTerrain terrain,
            int column,
            int row,
            bool isHighGround = false)
        {
            var cellHeight = terrain == BoardTerrain.FrontYard
                ? FrontYardCellHeight
                : PoolAndRoofCellHeight;

            var origin = new Vector2(
                FirstLogicalOrigin.x + column * CellWidth,
                FirstLogicalOrigin.y - row * cellHeight);

            if (terrain == BoardTerrain.Roof)
            {
                // Original roof Y is row*85 + max(0, 5-column)*20 + LAWN_YMIN - 10.
                origin.y += 10f - Mathf.Max(0, 5 - column) * 20f;
            }

            if (isHighGround)
            {
                origin.y += HighGroundHeight;
            }

            return new BoardCellGeometry(
                origin,
                origin + new Vector2(CellWidth * 0.5f, -cellHeight * 0.5f),
                origin + PlantGroundOffset,
                new Vector2(CellWidth, cellHeight));
        }

        /// <summary>
        /// Mirrors PlantingPixelToGridX/Y for the board geometry itself. Plant-specific
        /// cursor Y adjustments are intentionally applied by the planting layer.
        /// </summary>
        public static bool TryLocalPositionToCell(
            BoardTerrain terrain,
            Vector2 localPosition,
            out Vector2Int point)
        {
            var pixelX = localPosition.x + GridPivotScreenX;
            var pixelY = GridPivotScreenY - localPosition.y;
            if (pixelX < LawnMinX || pixelX >= LawnMinX + ColumnCount * CellWidth || pixelY < LawnMinY)
            {
                point = default;
                return false;
            }

            var column = Mathf.FloorToInt((pixelX - LawnMinX) / CellWidth);
            if (terrain == BoardTerrain.Roof && column < 5)
            {
                pixelY -= (4 - column) * 20f;
            }

            var rowHeight = terrain == BoardTerrain.FrontYard
                ? FrontYardCellHeight
                : PoolAndRoofCellHeight;
            var row = Mathf.FloorToInt((pixelY - LawnMinY) / rowHeight);
            if (row < 0 || row >= GetRowCount(terrain))
            {
                point = default;
                return false;
            }

            point = new Vector2Int(column, row);
            return true;
        }
    }
}
