using System;
using System.Collections.Generic;
using System.Linq;

namespace HowFrame
{
    public class RefList<T> : IList<T>, IDisposable
    {
        private readonly List<T> _list = new List<T>();

        public Action<T, int> OnAdd;
        public Action<T, int> OnAddDone;
        public Action<T, int> OnInsert;
        public Action<T, int> OnInsertDone;
        public Action<T, int> OnRemove;
        public Action<T, int> OnRemoveDone;
        public Action<T, int> OnRemoveAt;
        public Action<T, int> OnRemoveAtDone;
        public Action OnClear;
        public Action OnClearDone;
        public Action<T, int> OnSet;
        public Action<T, int> OnSetDone;

        public int Count => _list.Count;
        public bool IsReadOnly => false;
        public T this[int index]
        {
            get => _list[index];
            set
            {
                if (index >= 0 && index < _list.Count)
                {
                    T oldItem = _list[index];
                    OnSet?.Invoke(oldItem, index);
                    _list[index] = value;
                    OnSetDone?.Invoke(value, index);
                }
            }
        }

        public void Add(T item)
        {
            int index = _list.Count;
            OnAdd?.Invoke(item, index);
            _list.Add(item);
            OnAddDone?.Invoke(item, index);
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public void Insert(int index, T item)
        {
            OnInsert?.Invoke(item, index);
            _list.Insert(index, item);
            OnInsertDone?.Invoke(item, index);
        }

        public void InsertRange(int index, IEnumerable<T> items)
        {
            int currentIndex = index;
            foreach (var item in items)
            {
                Insert(currentIndex, item);
                currentIndex++;
            }
        }

        public bool Remove(T item)
        {
            int index = _list.IndexOf(item);
            if (index < 0) return false;

            OnRemove?.Invoke(item, index);
            bool result = _list.Remove(item);
            if (result)
            {
                OnRemoveDone?.Invoke(item, index);
            }
            return result;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _list.Count) return;

            T item = _list[index];
            OnRemoveAt?.Invoke(item, index);
            _list.RemoveAt(index);
            OnRemoveAtDone?.Invoke(item, index);
        }

        public void RemoveRange(int index, int count)
        {
            if (index < 0 || count < 0 || index + count > _list.Count) return;

            for (int i = 0; i < count; i++)
            {
                RemoveAt(index);
            }
        }

        public void Clear()
        {
            OnClear?.Invoke();
            _list.Clear();
            OnClearDone?.Invoke();
        }

        public bool Contains(T item) => _list.Contains(item);

        public int IndexOf(T item) => _list.IndexOf(item);

        public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        public void Dispose()
        {
            OnAdd = null;
            OnAddDone = null;
            OnInsert = null;
            OnInsertDone = null;
            OnRemove = null;
            OnRemoveDone = null;
            OnRemoveAt = null;
            OnRemoveAtDone = null;
            OnClear = null;
            OnClearDone = null;
            OnSet = null;
            OnSetDone = null;
        }
    }
}
