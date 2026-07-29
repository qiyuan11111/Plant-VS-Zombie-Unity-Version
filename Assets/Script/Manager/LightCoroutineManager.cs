using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Script.Manager
{
    public class LightCoroutineManager : MonoBehaviour
    {
        public static LightCoroutineManager Instance;
        private readonly Dictionary<string, IEnumerator> _coroutines = new Dictionary<string, IEnumerator>();
        private readonly List<string> _coroutineKeys = new List<string>();
        private readonly List<string> _toRemove = new List<string>();

        private void Awake()
        {
            Instance = this;
        }

        public string StartLightCoroutine(string name, IEnumerator routine)
        {
            var taskId = $"{name}:{Guid.NewGuid():N}";
            if (routine == null) return taskId;

            try
            {
                if (routine.MoveNext()) _coroutines.Add(taskId, routine);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            return taskId;
        }

        public bool Exist(string name)
        {
            return !string.IsNullOrEmpty(name) && _coroutines.ContainsKey(name);
        }

        public static void Stop(string name)
        {
            if (Instance == null || string.IsNullOrEmpty(name) ||
                !Instance._coroutines.TryGetValue(name, out var routine)) return;
            // 回收正在等待的对象
            if (routine.Current is WaitForSecondsPool wait)
            {
                wait.Recycle();
            }

            Instance._coroutines.Remove(name);
        }

        private void Update()
        {
            _coroutineKeys.Clear();
            _coroutineKeys.AddRange(_coroutines.Keys);
            _toRemove.Clear();

            foreach (var taskId in _coroutineKeys)
            {
                if (_coroutines.TryGetValue(taskId, out var routine) && !ProcessCoroutine(routine))
                {
                    _toRemove.Add(taskId);
                }
            }

            foreach (var taskId in _toRemove)
            {
                _coroutines.Remove(taskId);
            }
        }

        private static bool ProcessCoroutine(IEnumerator coroutine)
        {
            try
            {
                var current = coroutine.Current;
                if (current is WaitForSecondsPool wait)
                {
                    if (!wait.IsDone) return true;
                    wait.Recycle();
                }
                else if (current != null)
                {
                    Debug.LogError($"LightCoroutine 不支持等待类型: {current.GetType().FullName}");
                    return false;
                }

                // 推进协程并检查结束状态
                return coroutine.MoveNext();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }

        private void OnDestroy()
        {
            foreach (var routine in _coroutines.Values)
            {
                if (routine.Current is WaitForSecondsPool wait) wait.Recycle();
            }

            _coroutines.Clear();
            if (Instance == this) Instance = null;
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
