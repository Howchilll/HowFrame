using System;
using System.Collections.Generic;
using UnityEngine;
using HowEnum;

namespace HowFrame
{
    // ---------------- 无参数事件对象版 ----------------
    public class EventHelper
    {
        private readonly Dictionary<string, Action> _stringEvents = new();

        // ---------- 字符串 Key ----------
        public void Subscribe(string key, Action listener)
        {
            if (!_stringEvents.ContainsKey(key)) _stringEvents[key] = listener;
            else _stringEvents[key] += listener;
        }

        public void Unsubscribe(string key, Action listener)
        {
            if (_stringEvents.ContainsKey(key))
            {
                _stringEvents[key] -= listener;
                if (_stringEvents[key] == null) _stringEvents.Remove(key);
            }
        }

        public void Invoke(string key)
        {
            if (_stringEvents.TryGetValue(key, out var action)) action?.Invoke();
#if UNITY_EDITOR
            else Debug.LogWarning($"[EventHelper] 字符串事件未注册: {key}");
#endif
        }

        public void ClearOne(string key)
        {
            if (_stringEvents.ContainsKey(key)) _stringEvents.Remove(key);
        }

        public void ClearAll() => _stringEvents.Clear();

        // ---------- EnumKeyBase Key ----------
        public void Subscribe(EnumKeyBase key, Action listener)
        {
            Subscribe(key.name, listener);
        }

        public void Unsubscribe(EnumKeyBase key, Action listener)
        {
            Unsubscribe(key.name, listener);
        }

        public void Invoke(EnumKeyBase key)
        {
            Invoke(key.name);
        }

        public void ClearOne(EnumKeyBase key)
        {
            ClearOne(key.name);
        }
    }

    // ---------------- 泛型事件对象版 ----------------
    public class EventHelper<T, TResult>
    {
        private readonly Dictionary<string, Func<T, TResult>> _stringEvents = new();

        // ---------- 字符串 Key ----------
        public void Subscribe(string key, Func<T, TResult> func)
        {
            if (_stringEvents.ContainsKey(key))
                _stringEvents[key] += func;
            else
                _stringEvents[key] = func;
        }

        public void Unsubscribe(string key, Func<T, TResult> func)
        {
            if (_stringEvents.ContainsKey(key))
            {
                _stringEvents[key] -= func;
                if (_stringEvents[key] == null) _stringEvents.Remove(key);
            }
        }

        public TResult Invoke(string key, T arg)
        {
            if (_stringEvents.TryGetValue(key, out var func)) return func.Invoke(arg);

#if UNITY_EDITOR
            Debug.LogWarning($"[EventHelper<{typeof(T).Name},{typeof(TResult).Name}>] 字符串事件未注册: {key}");
#endif
            return default;
        }

        public void ClearOne(string key)
        {
            if (_stringEvents.ContainsKey(key)) _stringEvents.Remove(key);
        }

        public void ClearAll() => _stringEvents.Clear();

        // ---------- EnumKeyBase Key ----------
        public void Subscribe(EnumKeyBase key, Func<T, TResult> func)
        {
            Subscribe(key.name, func);
        }

        public void Unsubscribe(EnumKeyBase key, Func<T, TResult> func)
        {
            Unsubscribe(key.name, func);
        }

        public TResult Invoke(EnumKeyBase key, T arg)
        {
            return Invoke(key.name, arg);
        }

        public void ClearOne(EnumKeyBase key)
        {
            ClearOne(key.name);
        }
    }
}
