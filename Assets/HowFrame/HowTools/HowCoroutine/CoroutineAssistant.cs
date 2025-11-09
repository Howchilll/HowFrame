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
        private static readonly Dictionary<EnumKeyBase, Coroutine> _enumCoroutines = new();
        private static readonly FakeMono Runner;

        static CoroutineAssistant()
        {
            var go = new GameObject("[CoroutineAssistant]");
            UnityEngine.Object.DontDestroyOnLoad(go);
            Runner = go.AddComponent<FakeMono>();
        }

        #region String Key Versions

        public static void StartLoop(string name, float interval, Action onTick, Action onStart = null)
        {
            Stop(name);
            Coroutine coroutine = Runner.StartCoroutine(RunLoop(interval, onTick, onStart, name, _coroutines));
            _coroutines[name] = coroutine;
        }

        public static void StartLoop(string name, int loopCount, float interval, Action onLoop, Action onStart = null, Action onComplete = null)
        {
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
            Stop(name);
            if (delay <= 0f) delay = Time.deltaTime;
            Coroutine coroutine = Runner.StartCoroutine(DelayCoroutine(name, delay, onComplete, _coroutines));
            _coroutines[name] = coroutine;
        }

        #endregion

        #region EnumKeyBase Versions

        public static void StartLoop(EnumKeyBase key, float interval, Action onTick, Action onStart = null)
        {
            Stop(key);
            Coroutine coroutine = Runner.StartCoroutine(RunLoop(interval, onTick, onStart, key, _enumCoroutines));
            _enumCoroutines[key] = coroutine;
        }

        public static void StartLoop(EnumKeyBase key, int loopCount, float interval, Action onLoop, Action onStart = null, Action onComplete = null)
        {
            Stop(key);
            Coroutine coroutine = Runner.StartCoroutine(LoopCoroutine(key, loopCount, interval, onLoop, onStart, onComplete, _enumCoroutines));
            _enumCoroutines[key] = coroutine;
        }

        public static void Stop(EnumKeyBase key)
        {
            if (_enumCoroutines.TryGetValue(key, out Coroutine coroutine))
            {
                if (Runner != null) Runner.StopCoroutine(coroutine);
                _enumCoroutines.Remove(key);
            }
        }

        public static void DelayInvoke(EnumKeyBase key, float delay, Action onComplete)
        {
            Stop(key);
            if (delay <= 0f) delay = Time.deltaTime;
            Coroutine coroutine = Runner.StartCoroutine(DelayCoroutine(key, delay, onComplete, _enumCoroutines));
            _enumCoroutines[key] = coroutine;
        }

        #endregion
        
        #region Anonymous Version

        public static void DelayInvoke(float delay, Action onComplete)
        {
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

        private static IEnumerator RunLoop(float interval, Action onTick, Action onStart, EnumKeyBase key, Dictionary<EnumKeyBase, Coroutine> dict)
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

        private static IEnumerator LoopCoroutine(EnumKeyBase key, int loopCount, float interval, Action onLoop, Action onStart, Action onComplete, Dictionary<EnumKeyBase, Coroutine> dict)
        {
            onStart?.Invoke();
            for (int i = 0; i < loopCount; i++)
            {
                onLoop?.Invoke();
                yield return new WaitForSeconds(interval);
            }
            onComplete?.Invoke();
            dict.Remove(key);
        }

        private static IEnumerator DelayCoroutine(string name, float delay, Action onComplete, Dictionary<string, Coroutine> dict)
        {
            yield return new WaitForSeconds(delay);
            onComplete?.Invoke();
            dict.Remove(name);
        }

        private static IEnumerator DelayCoroutine(EnumKeyBase key, float delay, Action onComplete, Dictionary<EnumKeyBase, Coroutine> dict)
        {
            yield return new WaitForSeconds(delay);
            onComplete?.Invoke();
            dict.Remove(key);
        }

        #endregion

        private class FakeMono : MonoBehaviour { }
    }
}
