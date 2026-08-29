using PvZ.Gameplay.Plants;
using UnityEngine;

namespace PvZ.Gameplay.Board
{
    public sealed class BoardCell
    {
        private PlantEntity _occupant;
        private readonly bool _acceptsOccupant;

        public BoardCell(Vector2Int point, Vector2 position)
            : this(point, position, true)
        {
        }

        public BoardCell(Vector2Int point, BoardCellGeometry geometry)
            : this(point, geometry.Center, true)
        {
            LogicalOrigin = geometry.LogicalOrigin;
            Size = geometry.Size;
        }

        private BoardCell(Vector2Int point, Vector2 position, bool acceptsOccupant)
        {
            Point = point;
            Position = position;
            LogicalOrigin = position + new Vector2(-40f, 50f);
            Size = new Vector2(80f, 100f);
            _acceptsOccupant = acceptsOccupant;
        }

        public static BoardCell None { get; } = new(
            new Vector2Int(-1, -1),
            new Vector2(-1, -1),
            false);

        public Vector2Int Point { get; }
        public Vector2 Position { get; }
        public Vector2 LogicalOrigin { get; private set; }
        public Vector2 Size { get; private set; }
        public PlantEntity Occupant => _occupant;
        public bool IsOccupied => _occupant != null;

        public bool TryOccupy(PlantEntity plant)
        {
            if (!_acceptsOccupant || IsOccupied || plant == null) return false;
            _occupant = plant;
            return true;
        }

        public bool TryRelease(PlantEntity plant)
        {
            if (_occupant != plant) return false;
            _occupant = null;
            return true;
        }

        public bool Contains(Vector2 localPosition)
        {
            var delta = localPosition - Position;
            return Mathf.Abs(delta.x) < Size.x * 0.5f &&
                   Mathf.Abs(delta.y) < Size.y * 0.5f;
        }
    }
}
