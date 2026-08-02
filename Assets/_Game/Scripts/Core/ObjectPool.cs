using System;
using System.Collections.Generic;

namespace DinnerRush
{
    /// <summary>
    /// Generic reuse pool. Factory creates instances on demand; returned
    /// instances are reused. Optional onGet/onReturn hooks (e.g. enable/disable).
    /// </summary>
    public class ObjectPool<T> where T : class
    {
        private readonly Stack<T> _available = new Stack<T>();
        private readonly Func<T> _factory;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onReturn;

        public ObjectPool(Func<T> factory, Action<T> onGet = null, Action<T> onReturn = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _onGet = onGet;
            _onReturn = onReturn;
        }

        public int AvailableCount => _available.Count;

        public T Get()
        {
            T item = _available.Count > 0 ? _available.Pop() : _factory();
            _onGet?.Invoke(item);
            return item;
        }

        public void Return(T item)
        {
            _onReturn?.Invoke(item);
            _available.Push(item);
        }
    }
}
