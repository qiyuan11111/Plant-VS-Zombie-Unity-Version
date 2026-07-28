using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Script.Manager
{
    public class LightCoroutineManager : MonoBehaviour
    {
        public static LightCoroutineManager Instance;
        private readonly Dictionary<string, IEnumerator> _coroutines = new Dictionary<string, IEnumerator>();

        private void Awake()
        {
            Instance = this;
        }

        public string StartLightCoroutine(string name, IEnumerator routine)
        {
            // 同名协程存在时先停止旧协程
            name = name + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + Guid.NewGuid().ToString("N")[..8];
            if (Instance._coroutines.ContainsKey(name))
            {
                Stop(name);
            }
            if (routine.MoveNext()) Instance._coroutines.Add(name, routine);
            return name;
        }

        public bool Exist(string name)
        {
            return _coroutines.ContainsKey(name);
        }

        public static void Stop(string name)
        {
            if (Instance == null || !Instance._coroutines.TryGetValue(name, out var routine)) return;
            // 回收正在等待的对象
            if (routine.Current is WaitForSecondsPool wait)
            {
                wait.Recycle();
            }

            Instance._coroutines.Remove(name);
        }

        private void Update()
        {
            var toRemove = (from pair in _coroutines where !ProcessCoroutine(pair.Value) select pair.Key).ToList();
            // Debug.Log(toRemove.Count);
            foreach (var corName in toRemove)
            {
                _coroutines.Remove(corName);
            }
        }

        private static bool ProcessCoroutine(IEnumerator coroutine)
        {
            try
            {
                var current = coroutine.Current;
                switch (current)
                {
                    case null:
                        return false;
                    // 处理时间等待对象
                    case WaitForSecondsPool { IsDone: false }:
                        return true; // 未完成等待
                    // 等待完成时回收对象
                    case WaitForSecondsPool wait:
                        wait.Recycle();
                        break;
                }
                
                // 推进协程并检查结束状态
                if (coroutine.MoveNext()) return true;
                // 协程自然结束时回收最后等待对象
                // if (current is WaitForSecondsPool finalWait)
                // {
                //     finalWait.Recycle();
                // }

                return false;
            }
            catch (Exception e)
            {
                // Debug.LogError($"协程异常终止: {e}");
                return false;
            }
        }
    }

    public class WaitForSecondsPool
    {
        private static readonly Stack<WaitForSecondsPool> _pool = new();

        private float _targetTime;
        private bool _isInPool;

        private WaitForSecondsPool()
        {
        }

        public static WaitForSecondsPool Create(float seconds)
        {
            
            if (_pool.Count == 0)
            {
                
                return new WaitForSecondsPool { _targetTime = TimeManager.Instance.globalTime + seconds };
            }

            var obj = _pool.Pop();
            obj._targetTime = TimeManager.Instance.globalTime + seconds; // 重置为新的时间
            obj._isInPool = false;
            
            return obj;
        }

        public void Recycle()
        {
            if (_isInPool) return;

            _isInPool = true;
            _pool.Push(this);
        }

        // public bool IsDone => TimeManager.Instance.globalTime >= _targetTime;
        public bool IsDone => TimeManager.Instance.globalTime >= _targetTime;
    }
}