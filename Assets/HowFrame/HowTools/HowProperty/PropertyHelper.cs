using System;
using System.Collections.Generic;
using HowEnum;

namespace HowFrame
{
    public class PropertyHelper
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
            private readonly PropertyHelper _assistant;
            private readonly string _key;

            public BindHelper(PropertyHelper assistant, string key)
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

        public BindHelper<T> SetObj<T>(string key, Ref<T> obj)
        {

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
            foreach (var e in _enumDict.Values)
                e.Unbind();

            _enumDict.Clear();
            _dict.Clear();
        }
        
        
            private readonly Dictionary<EnumKeyBase, EntryBase> _enumDict = new Dictionary<EnumKeyBase, EntryBase>();

        // 链式绑定代理（枚举版）
        public class EnumBindHelper<T>
        {
            private readonly PropertyHelper _assistant;
            private readonly EnumKeyBase _key;

            public EnumBindHelper(PropertyHelper assistant, EnumKeyBase key)
            {
                _assistant = assistant;
                _key = key;
            }

            public void OnChange(Action<T> callback)
            {
                _assistant.SetEvent(_key, callback);
            }
        }

        // 设置事件（枚举 key 版）
        public void SetEvent<T>(EnumKeyBase key, Action<T> callback)
        {
            if (!_enumDict.TryGetValue(key, out var baseEntry))
            {
                baseEntry = new Entry<T>();
                _enumDict[key] = baseEntry;
            }

            var entry = (Entry<T>)baseEntry;
            entry.Callback = callback;

            if (entry.Obj != null)
                entry.Obj.OnChanged += callback;
        }

        // 设置对象（枚举 key 版）
        public EnumBindHelper<T> SetObj<T>(EnumKeyBase key, Ref<T> obj)
        {

            if (!_enumDict.TryGetValue(key, out var baseEntry))
            {
                baseEntry = new Entry<T>();
                _enumDict[key] = baseEntry;
            }

            var entry = (Entry<T>)baseEntry;
            entry.Obj = obj;

            if (entry.Callback != null)
                obj.OnChanged += entry.Callback;

            return new EnumBindHelper<T>(this, key);
        }

        // 移除绑定（枚举 key 版）
        public void Remove(EnumKeyBase key)
        {
            if (_enumDict.TryGetValue(key, out var baseEntry))
            {
                baseEntry.Unbind();
                _enumDict.Remove(key);
            }
        }

        ~PropertyHelper()
        {
            ClearAll();
        }
    }
        
        
}
    
    
    

