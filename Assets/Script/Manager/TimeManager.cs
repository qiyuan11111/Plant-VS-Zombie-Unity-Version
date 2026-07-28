using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Script.Manager
{
    public class TimeManager : MonoBehaviour
    {
        // Start is called before the first frame update
        public static TimeManager Instance;
    
        public float globalTime = 0;

        private void Awake()
        {
            Instance = this;
        }

        // Update is called once per frame
        private void Update()
        {
            globalTime += Time.deltaTime;
        }
    }
}
