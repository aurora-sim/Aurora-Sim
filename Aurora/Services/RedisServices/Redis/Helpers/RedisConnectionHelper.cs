using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Aurora.RedisServices.ConnectionHelpers
{
    public class Pool<T>
    {
        private readonly List<T> items = new List<T>();
        private readonly Queue<T> freeItems = new Queue<T>();
        private volatile object _lock = new object();
        private readonly Func<T> createItemAction;

        public Pool(Func<T> createItemAction)
        {
            this.createItemAction = createItemAction;
        }

        public void FlagFreeItem(T item)
        {
            lock(_lock)
                freeItems.Enqueue(item);
        }

        public void DestroyItem(T item)
        {
            lock (_lock)
                items.Remove(item);
        }

        public T GetFreeItem()
        {
            lock (_lock)
            {
                if (freeItems.Count == 0)
                {
                    T item = createItemAction();
                    items.Add(item);

                    return item;
                }

                return freeItems.Dequeue();
            }
        }

        public List<T> Items
        {
            get { return items; }
        }

        public void Clear()
        {
            items.Clear();
            freeItems.Clear();
        }
    }
}
