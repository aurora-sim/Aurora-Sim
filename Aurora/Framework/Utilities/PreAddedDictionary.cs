/*
 * Copyright (c) Contributors, http://aurora-sim.org/
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

using System.Collections.Generic;
using System.Linq;

namespace Aurora.Framework.Utilities
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