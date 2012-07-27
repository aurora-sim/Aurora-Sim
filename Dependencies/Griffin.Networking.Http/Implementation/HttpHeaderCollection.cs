using System;
using System.Collections;
using System.Collections.Generic;
using Griffin.Networking.Http.Protocol;

namespace Griffin.Networking.Http.Implementation
{
    internal class HttpHeaderCollection : IHeaderCollection
    {
        readonly Dictionary<string, HttpHeaderItem> _items = new Dictionary<string, HttpHeaderItem>(StringComparer.OrdinalIgnoreCase);

        public void Add(string name, string value)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (value == null) throw new ArgumentNullException("value");

            HttpHeaderItem header;
            if (_items.TryGetValue(name, out header))
            {
                header.AddValue(value);
            }
            else
                _items.Add(name, new HttpHeaderItem(name, value));
        }

        public void Remove(string name)
        {
            if (name == null) throw new ArgumentNullException("name");

            _items.Remove(name);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<IHeaderItem> GetEnumerator()
        {
#if NET_3_5
            List<IHeaderItem> headers = new List<IHeaderItem>();
            foreach (var item in _items.Values)
                headers.Add(item);
            return headers.GetEnumerator() as IEnumerator<IHeaderItem>;
#else
            return _items.Values.GetEnumerator();
#endif
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Gets a header
        /// </summary>
        /// <param name="name">header name.</param>
        /// <returns>value if found; otherwise <c>null</c>.</returns>
        public IHeaderItem this[string name]
        {
            get
            {
                HttpHeaderItem header;
                return !_items.TryGetValue(name, out header) ? null : header;
            }
            set
            {
                //LSP violation. (Got a solution which won't violate Law Of Demeter?)
                _items[name] = (HttpHeaderItem)value;
            }
        }
    }
}