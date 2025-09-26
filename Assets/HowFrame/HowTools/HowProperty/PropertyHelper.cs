using System;
using System.Collections.Generic;

namespace HowFrame
{
    public class PropertyAssistant
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
                if (Obj != null && Callback != null)
                    Obj.OnChanged -= Callback;
            }
        }

        private readonly Dictionary<string, EntryBase> _dict = new Dictionary<string, EntryBase>();

        // 链式绑定代理
        public class BindHelper<T>
        {
            private readonly PropertyAssistant _assistant;
            private readonly string _key;

            public BindHelper(PropertyAssistant assistant, string key)
            {
                _assistant = assistant;
                _key = key;
            }

            public void OnChange(Action<T> callback)
            {
                _assistant.SetEvent<T>(_key, callback);
            }
        }

        public void SetEvent<T>(string key, Action<T> callback)
        {
            if (!_dict.TryGetValue(key, out var baseEntry))
            {
                baseEntry = new Entry<T>();
                _dict[key] = baseEntry;
            }

            var entry = (Entry<T>)baseEntry;
            entry.Callback = callback;

            if (entry.Obj != null)
                entry.Obj.OnChanged += callback;
        }

        public BindHelper<T> SetObj<T>(string key, Ref<Object> Obj)
        {
            if (!(Obj is Ref<T> obj))
                throw new InvalidCastException($"SetObj 类型不匹配：key={key}, T={typeof(T)}, 实际={Obj.GetType()}");

            if (!_dict.TryGetValue(key, out var baseEntry))
            {
                baseEntry = new Entry<T>();
                _dict[key] = baseEntry;
            }

            var entry = (Entry<T>)baseEntry;
            entry.Obj = obj;

            if (entry.Callback != null)
                obj.OnChanged += entry.Callback;

            return new BindHelper<T>(this, key);
        }

        public void Remove(string key)
        {
            if (_dict.TryGetValue(key, out var baseEntry))
            {
                baseEntry.Unbind();
                _dict.Remove(key);
            }
        }

        public void ClearAll()
        {
            foreach (var e in _dict.Values)
                e.Unbind();

            _dict.Clear();
        }
    }
}
