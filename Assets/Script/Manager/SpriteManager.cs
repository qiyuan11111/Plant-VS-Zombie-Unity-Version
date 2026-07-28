using System.Collections.Generic;
using UnityEngine;

namespace Script.Manager
{
    public class SpriteManager : MonoBehaviour
    {
        public static SpriteManager Instance;
        
        //创建一个string到ulong的字典
        private readonly Dictionary<string, ulong> _sortingLayerPoint = new();
        
        public ulong GetSortingLayerNewOrder(string layername, int num)
        {
            if (!_sortingLayerPoint.ContainsKey(layername))
            {
                return 0;
            }
            var point = _sortingLayerPoint[layername];
            if (point + (ulong)num <= 10000000)
            {
                _sortingLayerPoint[layername] = point + (ulong)num;
                return point;
            }

            _sortingLayerPoint[layername] = 30;
            return 30;
        }

        void Awake()
        {
            Instance = this;
            _sortingLayerPoint.Add("plant", 30);
            _sortingLayerPoint.Add("zombie", 30);
            _sortingLayerPoint.Add("sun", 30);
        }
    }
}