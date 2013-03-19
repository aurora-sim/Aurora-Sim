using System.Collections.Generic;
using System.Linq;

namespace Aurora.Framework
{
    public class PreAddedDictionary<A, B> : IDictionary<A, B>
    {
        private Dictionary<A, B> _dict = new Dictionary<A, B>();
        private object _lock = new object();
        private Creator _creator;

        public delegate B Creator();

        public PreAddedDictionary(Creator c)
        {
            _creator = c;
        }

        public void Add(A key, B value)
        {
            lock (_lock)
                _dict.Add(key, value);
        }

        public bool ContainsKey(A key)
        {
            lock (_lock)
                return _dict.ContainsKey(key);
        }

        public ICollection<A> Keys
        {
            get
            {
                lock (_lock)
                    return _dict.Keys;
            }
        }

        public bool Remove(A key)
        {
            lock (_lock)
                return _dict.Remove(key);
        }

        public bool TryGetValue(A key, out B value)
        {
            lock (_lock)
                return _dict.TryGetValue(key, out value);
        }

        public ICollection<B> Values
        {
            get
            {
                lock (_lock)
                    return _dict.Values;
            }
        }

        public B this[A key]
        {
            get
            {
                lock (_lock)
                {
                    if (!_dict.ContainsKey(key))
                        _dict.Add(key, _creator());
                    return _dict[key];
                }
            }
            set
            {
                lock (_lock)
                    _dict[key] = value;
            }
        }

        public void Add(KeyValuePair<A, B> item)
        {
            lock (_lock)
                _dict.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            lock (_lock)
                _dict.Clear();
        }

        public bool Contains(KeyValuePair<A, B> item)
        {
            lock (_lock)
                return _dict.Contains(item);
        }

        public void CopyTo(KeyValuePair<A, B>[] array, int arrayIndex)
        {
        }

        public int Count
        {
            get
            {
                lock (_lock)
                    return _dict.Count;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<A, B> item)
        {
            lock (_lock)
                return _dict.Remove(item.Key);
        }

        public IEnumerator<KeyValuePair<A, B>> GetEnumerator()
        {
            lock (_lock)
                return _dict.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            lock (_lock)
                return _dict.GetEnumerator();
        }
    }
}