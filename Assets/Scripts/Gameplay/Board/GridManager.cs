using System;
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
        private static readonly List<float> XAxis = new()
        {
            -401f,
            -321.3f, //162
            -241f, //159
            -161f, //161
            -81f, //161
            1f, //163
            78.5f, //158
            160.5f, //37.1
            239.5f, //38.5
        };

        private static readonly List<float> YAxis = new()
        {
            168.5f, //(340)
            68f, //-200
            -32f, //201
            -132.5f, //201
            -234.2f
        };

        private int _rowNum, _colNum;

        private Grid[,] _gridMap;

        private void CreatGrids()
        {
            for (var i = 0; i < XAxis.Count; i++)
            {
                for (var j = 0; j < YAxis.Count; j++)
                {
                    float x = XAxis[i], y = YAxis[j];
                    var point = new Vector2Int(i, j);
                    var position = new Vector2(x, y);
                    var grid = new Grid(point, position);
                    _gridMap[i, j] = grid;
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
            int row = -1, col = -1;
            Vector2 localPosition = transform.InverseTransformPoint(worldPosition);

            var minDistance = float.MaxValue;
            for (var i = 0; i < XAxis.Count; i++)
            {
                var distance = Math.Abs(localPosition.x - XAxis[i]);
                if (!(distance < 41f) || !(distance < minDistance)) continue;
                minDistance = distance;
                row = i;
            }

            minDistance = float.MaxValue;
            for (var i = 0; i < YAxis.Count; i++)
            {
                var distance = Math.Abs(localPosition.y - YAxis[i]);
                if (!(distance < 51f) || !(distance < minDistance)) continue;
                minDistance = distance;
                col = i;
            }


            if (row <= -1 || col <= -1 || row >= _rowNum || col >= _colNum)
            {
                return Grid.None;
            }

            return _gridMap[row, col];
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
            _rowNum = XAxis.Count;
            _colNum = YAxis.Count;
            _gridMap = new Grid[_rowNum, _colNum];

            CreatGrids();
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

            private Grid(Vector2Int point, Vector2 position, bool acceptsOccupant)
            {
                Point = point;
                Position = position;
                _acceptsOccupant = acceptsOccupant;
            }
        }
    }
}
