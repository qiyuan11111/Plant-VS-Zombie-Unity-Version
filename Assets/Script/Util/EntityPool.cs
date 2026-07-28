using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Script
{
    public class EntityPool
    {
        private Queue<GameObject> Pool = new();
        
        public GameObject Get()
        {
            if (Empty()) return null;
            GameObject entity = Pool.Dequeue();
            entity.SetActive(true);
            return entity;
        }

        public bool Empty()
        {
            return Pool.Count == 0;
        }
    }
    
    
}