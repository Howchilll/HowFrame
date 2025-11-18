using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HowEnum;

namespace HowFrame
{
    public static class CoroutineAssistant
    {
        private static readonly Dictionary<string, Coroutine> _coroutines = new();
        private static FakeMono Runner;
        private static bool _initialized = false;

        private static void EnsureInitialized()
        {
            if (Runner == null)
            {
                Debug.LogError("CoroutineAssistant: 未初始化，请先调用 Wake()");
                Wake(); // 自动初始化以保持向后兼容
            }
        }

        #region String Key Versions

        public static void StartLoop(string name, float interval, Action onTick, Action onStart = null)
        {
            EnsureInitialized();
            Stop(name);
            Coroutine coroutine = Runner.StartCoroutine(RunLoop(interval, onTick, onStart, name, _coroutines));
            _coroutines[name] = coroutine;
        }

        public static void StartLoop(string name, int loopCount, float interval, Action onLoop, Action onStart = null, Action onComplete = null)
        {
            EnsureInitialized();
            Stop(name);
            Coroutine coroutine = Runner.StartCoroutine(LoopCoroutine(name, loopCount, interval, onLoop, onStart, onComplete, _coroutines));
            _coroutines[name] = coroutine;
        }

        public static void Stop(string name)
        {
            if (_coroutines.TryGetValue(name, out Coroutine coroutine))
            {
                if (Runner != null) Runner.StopCoroutine(coroutine);
                _coroutines.Remove(name);
            }
        }

        public static void DelayInvoke(string name, float delay, Action onComplete)
        {
            EnsureInitialized();
            Stop(name);
            if (delay <= 0f) delay = Time.deltaTime;
            Coroutine coroutine = Runner.StartCoroutine(DelayCoroutine(name, delay, onComplete, _coroutines));
            _coroutines[name] = coroutine;
        }

        #endregion

        #region EnumKeyBase Versions

        public static void StartLoop(EnumKeyBase key, float interval, Action onTick, Action onStart = null)
        {
            StartLoop(key.name, interval, onTick, onStart);
        }

        public static void StartLoop(EnumKeyBase key, int loopCount, float interval, Action onLoop, Action onStart = null, Action onComplete = null)
        {
            StartLoop(key.name, loopCount, interval, onLoop, onStart, onComplete);
        }

        public static void Stop(EnumKeyBase key)
        {
            Stop(key.name);
        }

        public static void DelayInvoke(EnumKeyBase key, float delay, Action onComplete)
        {
            DelayInvoke(key.name, delay, onComplete);
        }

        #endregion
        
        #region Anonymous Version

        public static void DelayInvoke(float delay, Action onComplete)
        {
            EnsureInitialized();
            if (delay <= 0f) delay = Time.deltaTime;
            Runner.StartCoroutine(DelayAnonymous(delay, onComplete));
        }

        private static IEnumerator DelayAnonymous(float delay, Action onComplete)
        {
            yield return new WaitForSeconds(delay);
            onComplete?.Invoke();
        }

        #endregion
        
        #region Core Coroutines

        private static IEnumerator RunLoop(float interval, Action onTick, Action onStart, string name, Dictionary<string, Coroutine> dict)
        {
            onStart?.Invoke();
            while (true)
            {
                onTick?.Invoke();
                yield return new WaitForSeconds(interval);
            }
        }

        private static IEnumerator LoopCoroutine(string name, int loopCount, float interval, Action onLoop, Action onStart, Action onComplete, Dictionary<string, Coroutine> dict)
        {
            onStart?.Invoke();
            for (int i = 0; i < loopCount; i++)
            {
                onLoop?.Invoke();
                yield return new WaitForSeconds(interval);
            }
            onComplete?.Invoke();
            dict.Remove(name);
        }

        private static IEnumerator DelayCoroutine(string name, float delay, Action onComplete, Dictionary<string, Coroutine> dict)
        {
            yield return new WaitForSeconds(delay);
            onComplete?.Invoke();
            dict.Remove(name);
        }

        #endregion

        /// <summary>
        /// 初始化 CoroutineAssistant（延迟初始化，在资源加载完成后调用）
        /// </summary>
        public static void Wake()
        {
            if (_initialized) return; // 防止重复初始化
            var go = new GameObject("[CoroutineAssistant]");
            UnityEngine.Object.DontDestroyOnLoad(go);
            Runner = go.AddComponent<FakeMono>();
            _initialized = true;
        }
        private class FakeMono : MonoBehaviour { }
    }
}
