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
}
