using UnityEngine;

namespace PvZ.Gameplay.Board
{
    public readonly struct BoardCellGeometry
    {
        public BoardCellGeometry(Vector2 logicalOrigin, Vector2 center, Vector2 size)
        {
            LogicalOrigin = logicalOrigin;
            Center = center;
            Size = size;
        }

        public Vector2 LogicalOrigin { get; }
        public Vector2 Center { get; }
        public Vector2 Size { get; }
    }
}
