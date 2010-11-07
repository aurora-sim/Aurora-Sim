using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.Framework
{
    public class DoubleValueDictionary<TKey, TValue1, TValue2>
    {
        Dictionary<TKey, List<object>> Dictionary = new Dictionary<TKey, List<object>>();

        public void Add(TKey key, TValue1 value1, TValue2 value2)
        {
            if (Dictionary.ContainsKey(key))
                throw new ArgumentException("Key is already in the dictionary");

            List<object> Values = new List<object>();
            Values[0] = value1;
            Values[1] = value2;
            Dictionary.Add(key, Values);
        }

        public bool Remove(TKey key)
        {
            if (!Dictionary.ContainsKey(key))
                return false;
            Dictionary.Remove(key);
            return true;
        }

        public TValue1 this[TKey key, TKey n]
        {
            get
            {
                if (!Dictionary.ContainsKey(key))
                    return default(TValue1);

                List<Object> Values = new List<object>();
                Dictionary.TryGetValue(key, out Values);
                return (TValue1)Values[0];
            }
            set
            {
                List<Object> Values = new List<object>();
                if (Dictionary.ContainsKey(key))
                    Dictionary.TryGetValue(key, out Values);

                Values[0] = value;
                Dictionary[key] = Values;
            }
        }

        public TValue2 this[TKey key]
        {
            get
            {
                if (!Dictionary.ContainsKey(key))
                    return default(TValue2);

                List<Object> Values = new List<object>();
                Dictionary.TryGetValue(key, out Values);
                return (TValue2)Values[1];
            }
            set
            {
                List<Object> Values = new List<object>();
                if (Dictionary.ContainsKey(key))
                    Dictionary.TryGetValue(key, out Values);

                Values[1] = value;
                Dictionary[key] = Values;
            }
        }

        public void Clear()
        {
            Dictionary.Clear();
        }

        public int Count
        {
            get { return Dictionary.Count; }
        }

        public bool ContainsKey(TKey key)
        {
            return Dictionary.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TValue1 value)
        {
            value = default(TValue1);
            if (!Dictionary.ContainsKey(key))
                return false;
            List<object> Values = new List<object>();
            Dictionary.TryGetValue(key, out Values);
            value = (TValue1)Values[0];
            return true;
        }

        public bool TryGetValue(TKey key, out TValue2 value)
        {
            value = default(TValue2);
            if (!Dictionary.ContainsKey(key))
                return false;
            List<object> Values = new List<object>();
            Dictionary.TryGetValue(key, out Values);
            value = (TValue2)Values[1];
            return true;
        }
    }

    //Fixed version of the LibOMV class
    public class DoubleKeyDictionary<TKey1, TKey2, TValue>
    {
        private object m_lock = new object();
        Dictionary<TKey1, TValue> Dictionary1 = new Dictionary<TKey1, TValue>();
        Dictionary<TKey2, TValue> Dictionary2 = new Dictionary<TKey2, TValue>();

        public void Add(TKey1 key1, TKey2 key2, TValue value)
        {
            lock (m_lock)
            {
                if (Dictionary1.ContainsKey(key1))
                    throw new ArgumentException("Key is already in the dictionary");
                if (Dictionary2.ContainsKey(key2))
                    throw new ArgumentException("Key is already in the dictionary");

                Dictionary1.Add(key1, value);
                Dictionary2.Add(key2, value);
            }
        }

        public bool Remove(TKey1 key1)
        {
            lock (m_lock)
            {
                if (!Dictionary1.ContainsKey(key1))
                    return false;
                Dictionary1.Remove(key1);
                return true;
            }
        }

        public bool Remove(TKey2 key2)
        {
            lock (m_lock)
            {
                if (!Dictionary2.ContainsKey(key2))
                    return false;
                Dictionary2.Remove(key2);
                return true;
            }
        }

        public void Clear()
        {
            lock (m_lock)
            {
                Dictionary1.Clear();
                Dictionary2.Clear();
            }
        }

        public int Count
        {
            get { return Dictionary1.Count; }
        }

        public bool ContainsKey(TKey1 key)
        {
            lock (m_lock)
            {
                return Dictionary1.ContainsKey(key);
            }
        }

        public bool ContainsKey(TKey2 key)
        {
            lock (m_lock)
            {
                return Dictionary2.ContainsKey(key);
            }
        }

        public bool TryGetValue(TKey1 key, out TValue value)
        {
            lock (m_lock)
            {
                return Dictionary1.TryGetValue(key, out value);
            }
        }

        public bool TryGetValue(TKey2 key, out TValue value)
        {
            lock (m_lock)
            {
                return Dictionary2.TryGetValue(key, out value);
            }
        }

        public void ForEach(Action<TValue> action)
        {
            lock (m_lock)
            {
                foreach (TValue value in Dictionary1.Values)
                    action(value);
            }
        }

        public void ForEach(Action<KeyValuePair<TKey1, TValue>> action)
        {
            lock (m_lock)
            {
                foreach (KeyValuePair<TKey1, TValue> entry in Dictionary1)
                    action(entry);
            }
        }

        public void ForEach(Action<KeyValuePair<TKey2, TValue>> action)
        {
            lock (m_lock)
            {
                foreach (KeyValuePair<TKey2, TValue> entry in Dictionary2)
                    action(entry);
            }
        }

        public TValue FindValue(Predicate<TValue> predicate)
        {
            lock (m_lock)
            {
                foreach (TValue value in Dictionary1.Values)
                {
                    if (predicate(value))
                        return value;
                }

                return default(TValue);
            }
        }

        public IList<TValue> FindAll(Predicate<TValue> predicate)
        {
            lock (m_lock)
            {
                IList<TValue> list = new List<TValue>();
                foreach (TValue value in Dictionary1.Values)
                {
                    if (predicate(value))
                        list.Add(value);
                }

                return list;
            }
        }

        public int RemoveAll(Predicate<TValue> predicate)
        {
            lock (m_lock)
            {
                IList<TKey1> list = new List<TKey1>();

                foreach (KeyValuePair<TKey1, TValue> kvp in Dictionary1)
                {
                    if (predicate(kvp.Value))
                        list.Add(kvp.Key);
                }

                IList<TKey2> list2 = new List<TKey2>(list.Count);
                foreach (KeyValuePair<TKey2, TValue> kvp in Dictionary2)
                {
                    if (predicate(kvp.Value))
                        list2.Add(kvp.Key);
                }

                for (int i = 0; i < list.Count; i++)
                    Dictionary1.Remove(list[i]);

                for (int i = 0; i < list2.Count; i++)
                    Dictionary2.Remove(list2[i]);

                return list.Count;
            }
        }
    }
}
