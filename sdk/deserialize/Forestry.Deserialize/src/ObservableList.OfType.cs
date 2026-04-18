using System.Collections;

namespace Forestry.Deserialize
{
    /// <summary>
    /// Observable list before and after operations
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ObservableList<T> : IList<T>
    {
        public ObservableList(
            IEnumerable<T>? items = null
        ) { 
            _items = items is null ? [] : [.. items]; 
        }

        protected readonly List<T> _items;

        public T this[int index] { 
            get => _items[index];
            set {
                ArgumentNullException.ThrowIfNull(value);

                Before();
                Before(value);

                _items[index] = value;

                After();
            } 
        }

        public int Count => _items.Count;

        public void Add(T item)
        {
            ArgumentNullException.ThrowIfNull(item);

            Before();
            Before(item);

            _items.Add(item);

            After();
        }

        public void Clear()
        {
            Before();

            _items.Clear();

            After();
        }

        public bool Contains(T item) => _items.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

        public int IndexOf(T item) => _items.IndexOf(item);

        public void Insert(int index, T item)
        {
            ArgumentNullException.ThrowIfNull(item);

            Before();
            Before(item);

            _items.Insert(index, item);

            After();
        }

        public bool Remove(T item)
        {
            Before();

            bool flag = _items.Remove(item);
            if (flag)
            {
                After();
            }

            return flag;
        }

        public void RemoveAt(int index)
        {
            Before();

            _items.RemoveAt(index);

            After();
        }

        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        public abstract bool IsReadOnly { get; }

        protected abstract void Before();

        protected virtual void Before(T item) { }

        protected virtual void After() { }
    }
}