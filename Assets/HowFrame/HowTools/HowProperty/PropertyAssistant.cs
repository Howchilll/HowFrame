using System;
using System.Collections.Generic;
using HowEnum;

namespace HowFrame
{
    public static class PropertyAssistant
    {
        private abstract class EntryBase
        {
            public abstract void Unbind();
        }

        private class Entry<T> : EntryBase
        {
            public Ref<T> Obj;
            public Action<T> Callback;

            public override void Unbind()
            {
                if (Obj != null && Callback != null) Obj.OnChanged -= Callback;
            }
        }

        private static readonly Dictionary<string, EntryBase> _dict = new Dictionary<string, EntryBase>();

        public class BindHelper<T>
        {
            private readonly string _key;

            public BindHelper(string key)
            {
                _key = key;
            }

            public void OnChange(Action<T> callback)
            {
                SetEvent<T>(_key, callback);
            }
        }

        public static void SetEvent<T>(string key, Action<T> callback)
        {
            if (!_dict.TryGetValue(key, out var baseEntry))
            {
                baseEntry = new Entry<T>();
                _dict[key] = baseEntry;
            }

            var entry = (Entry<T>)baseEntry;
            entry.Callback = callback;
            if (entry.Obj != null) entry.Obj.OnChanged += callback;
        }

        public static BindHelper<T> SetObj<T>(string key, Ref<T> obj)
        {
            if (!_dict.TryGetValue(key, out var baseEntry))
            {
                baseEntry = new Entry<T>();
                _dict[key] = baseEntry;
            }

            var entry = (Entry<T>)baseEntry;
            entry.Obj = obj;
            if (entry.Callback != null) obj.OnChanged += entry.Callback;
            return new BindHelper<T>(key);
        }

        public static void Remove(string key)
        {
            if (_dict.TryGetValue(key, out var entry))
            {
                entry.Unbind();
                _dict.Remove(key);
            }
        }

        public static void ClearAll()
        {
            foreach (var e in _dict.Values) e.Unbind();
            _dict.Clear();
        }

        // ----------- EnumKeyBase 版本 ----------- // 链式绑定代理

        public class EnumBindHelper<T>
        {
            private readonly string _key;

            public EnumBindHelper(string key)
            {
                _key = key;
            }

            public void OnChange(Action<T> callback)
            {
                SetEvent<T>(_key, callback);
            }
        }

        public static void SetEvent<T>(EnumKeyBase key, Action<T> callback)
        {
            SetEvent<T>(key.name, callback);
        }

        public static EnumBindHelper<T> SetObj<T>(EnumKeyBase key, Ref<T> obj)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key), "EnumKeyBase cannot be null");
            }
            if (string.IsNullOrEmpty(key.name))
            {
                throw new ArgumentException("EnumKeyBase.name cannot be null or empty. Make sure the EnumKey was created with a name parameter.", nameof(key));
            }
            SetObj<T>(key.name, obj);
            return new EnumBindHelper<T>(key.name);
        }

        public static void Remove(EnumKeyBase key)
        {
            Remove(key.name);
        }
        
        public static void Wake(){}
    }
}