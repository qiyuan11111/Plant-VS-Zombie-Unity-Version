using System;
using System.Collections.Generic;
using System.Linq;
using Script.Model;
using Script.Util;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace Script.Manager
{
    public class GridManager : MonoBehaviour, IPointerMoveHandler, IPointerClickHandler
    {
        public static GridManager Instance;

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

        private Grid GetGridByMouse()
        {
            int row = -1, col = -1;
            Vector2 mouse = transform.InverseTransformPoint(MainGameManager.Instance.GetNowMouseScreenToWorldPoint());

            var minDistance = float.MaxValue;
            for (var i = 0; i < XAxis.Count; i++)
            {
                var distance = Math.Abs(mouse.x - XAxis[i]);
                if (!(distance < 41f) || !(distance < minDistance)) continue;
                minDistance = distance;
                row = i;
            }

            minDistance = float.MaxValue;
            for (var i = 0; i < YAxis.Count(); i++)
            {
                var distance = Math.Abs(mouse.y - YAxis[i]);
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

        public void OnPointerMove(PointerEventData eventData)
        {
            var grid = GetGridByMouse();
            PlantingManager.Instance.SetCurrentChosenPoint(grid);
        }

        

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == 0)
            {
                PlantingManager.Instance.PlaceChosenPlant();
            }
            else
            {
                PlantingManager.Instance.CancelChoosePlantCard();
                // PlantingManager.Instance.SetCurrentChosenPoint(G);
            }
        }

        private void Awake()
        {
            Instance = this;

            _rowNum = XAxis.Count;
            _colNum = YAxis.Count;
            _gridMap = new Grid[_rowNum, _colNum];

            CreatGrids();
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

            private bool _isOccupied;

            public bool IsOccupied()
            {
                return _isOccupied;
            }

            private Grid SetOccupied(bool isOccupied)
            {
                _isOccupied = isOccupied;
                return this;
            }

            // private Entity Plante;
            private OnFieldCharacter _plante;

            public OnFieldCharacter GetOnFieldCharacter()
            {
                return _plante;
            }

            public Grid SetOnFieldCharacter(OnFieldCharacter character)
            {
                if (IsOccupied()) return this;

                SetOccupied(true);
                _plante = character;

                return this;
            }

            public static Grid None = new Grid(new Vector2Int(-1, -1), new Vector2(-1, -1));

            public Grid(Vector2Int point, Vector2 position, bool isOccupied = false)
            {
                this.Point = point;
                this.Position = position;
                this._isOccupied = isOccupied;
            }
        }
    }
}