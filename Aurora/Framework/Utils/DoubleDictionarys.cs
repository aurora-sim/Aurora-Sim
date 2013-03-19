/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Aurora.Framework
{
    public class DoubleValueDictionary<TKey, TValue1, TValue2>
    {
        private readonly Dictionary<TKey, List<object>> Dictionary = new Dictionary<TKey, List<object>>();

        public TValue1 this[TKey key, TKey n]
        {
            get
            {
                if (!Dictionary.ContainsKey(key))
                    return default(TValue1);

                List<Object> Values = new List<object>();
                Dictionary.TryGetValue(key, out Values);
                return (TValue1) Values[0];
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
                return (TValue2) Values[1];
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

        public int Count
        {
            get { return Dictionary.Count; }
        }

        public void Add(TKey key, TValue1 value1, TValue2 value2)
        {
            if (Dictionary.ContainsKey(key))
                throw new ArgumentException("Key is already in the dictionary");

            List<object> Values = new List<object>(2) {value1, value2};
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
            value = (TValue1) Values[0];
            return true;
        }

        public bool TryGetValue(TKey key, out TValue2 value)
        {
            value = default(TValue2);
            if (!Dictionary.ContainsKey(key))
                return false;
            List<object> Values = new List<object>();
            Dictionary.TryGetValue(key, out Values);
            value = (TValue2) Values[1];
            return true;
        }
    }

    //Fixed version of the LibOMV class
    public class DoubleKeyDictionary<TKey1, TKey2, TValue>
    {
        private readonly Dictionary<TKey1, TValue> Dictionary1 = new Dictionary<TKey1, TValue>();
        private readonly Dictionary<TKey2, TValue> Dictionary2 = new Dictionary<TKey2, TValue>();
        private readonly object m_lock = new object();

        public int Count
        {
            get { return Dictionary1.Count; }
        }

        public void Add(TKey1 key1, TKey2 key2, TValue value)
        {
            lock (m_lock)
            {
                if (Dictionary1.ContainsKey(key1))
                    throw new ArgumentException("Key1 (UUID, " + key1 + ") is already in the dictionary");
                if (Dictionary2.ContainsKey(key2))
                    throw new ArgumentException("Key2 (LocalID, " + key2 + ") is already in the dictionary");

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
                Dictionary1.TryGetValue(key, out value);
                return value != null;
            }
        }

        public bool TryGetValue(TKey2 key, out TValue value)
        {
            lock (m_lock)
            {
                Dictionary2.TryGetValue(key, out value);
                return value != null;
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
#if (!ISWIN)
                foreach (TValue value in Dictionary1.Values)
                {
                    if (predicate(value))
                    {
                        return value;
                    }
                }
#else
                foreach (TValue value in Dictionary1.Values.Where(value => predicate(value)))
                {
                    return value;
                }
#endif
                return default(TValue);
            }
        }

        public IList<TValue> FindAll(Predicate<TValue> predicate)
        {
            lock (m_lock)
            {
#if (!ISWIN)
                List<TValue> list = new List<TValue>();
                foreach (TValue value in Dictionary1.Values)
                {
                    if (predicate(value)) list.Add(value);
                }
                return list;
#else
                return Dictionary1.Values.Where(value => predicate(value)).ToList();
#endif
            }
        }

        public int RemoveAll(Predicate<TValue> predicate)
        {
            lock (m_lock)
            {
                IList<TKey1> list = (from kvp in Dictionary1 where predicate(kvp.Value) select kvp.Key).ToList();

                IList<TKey2> list2 = new List<TKey2>(list.Count);
#if (!ISWIN)
                foreach (KeyValuePair<TKey2, TValue> kvp in Dictionary2)
                {
                    if (predicate(kvp.Value))
                    {
                        list2.Add(kvp.Key);
                    }
                }
#else
                foreach (KeyValuePair<TKey2, TValue> kvp in Dictionary2.Where(kvp => predicate(kvp.Value)))
                {
                    list2.Add(kvp.Key);
                }
#endif

                foreach (TKey1 t in list)
                    Dictionary1.Remove(t);

                foreach (TKey2 t in list2)
                    Dictionary2.Remove(t);

                return list.Count;
            }
        }

        public TValue[] Values
        {
            get
            {
                lock (m_lock)
                {
                    TValue[] values = new TValue[Dictionary1.Count];
                    Dictionary1.Values.CopyTo(values, 0);
                    return values;
                }
            }
        }
    }
}