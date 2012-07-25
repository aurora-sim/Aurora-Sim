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

// PriorityQueue.cs
//
// Jim Mischel

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Mischel.Collections
{
    //A priority queue from http://www.devsource.com/c/a/Languages/A-Priority-Queue-Implementation-in-C/
    // Writen by Jim Mischel

    [Serializable]
    [ComVisible(false)]
    public struct PriorityQueueItem<TValue, TPriority>
    {
        public TPriority _priority;
        private TValue _value;

        public PriorityQueueItem(TValue val, TPriority pri)
        {
            this._value = val;
            this._priority = pri;
        }

        public TValue Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public TPriority Priority
        {
            get { return _priority; }
            set { _priority = value; }
        }
    }

    [Serializable]
    [ComVisible(false)]
    public class PriorityQueue<TValue, TPriority> : ICollection,
                                                    IEnumerable<PriorityQueueItem<TValue, TPriority>>
    {
        private const Int32 DefaultCapacity = 16;
        private Int32 capacity;

        private Comparison<TPriority> compareFunc;
        private PriorityQueueItem<TValue, TPriority>[] items;
        private Int32 numItems;

        /// <summary>
        ///   Initializes a new instance of the PriorityQueue class that is empty,
        ///   has the default initial capacity, and uses the default IComparer.
        /// </summary>
        public PriorityQueue()
            : this(DefaultCapacity, Comparer<TPriority>.Default)
        {
        }

        public PriorityQueue(Int32 initialCapacity)
            : this(initialCapacity, Comparer<TPriority>.Default)
        {
        }

        public PriorityQueue(IComparer<TPriority> comparer)
            : this(DefaultCapacity, comparer)
        {
        }

        public PriorityQueue(int initialCapacity, IComparer<TPriority> comparer)
        {
            Init(initialCapacity, comparer.Compare);
        }

        public PriorityQueue(Comparison<TPriority> comparison)
            : this(DefaultCapacity, comparison)
        {
        }

        public PriorityQueue(int initialCapacity, Comparison<TPriority> comparison)
        {
            Init(initialCapacity, comparison);
        }

        public PriorityQueueItem<TValue, TPriority>[] Items
        {
            get { return items; }
        }

        public int Capacity
        {
            get { return items.Length; }
            set { SetCapacity(value); }
        }

        #region ICollection Members

        public int Count
        {
            get { return numItems; }
        }

        public void CopyTo(Array array, int index)
        {
            this.CopyTo((PriorityQueueItem<TValue, TPriority>[]) array, index);
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return items.SyncRoot; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IEnumerable<PriorityQueueItem<TValue,TPriority>> Members

        public IEnumerator<PriorityQueueItem<TValue, TPriority>> GetEnumerator()
        {
            for (int i = 0; i < numItems; i++)
            {
                yield return items[i];
            }
        }

        #endregion

        private void Init(int initialCapacity, Comparison<TPriority> comparison)
        {
            numItems = 0;
            compareFunc = comparison;
            SetCapacity(initialCapacity);
        }

        private void SetCapacity(int newCapacity)
        {
            int newCap = newCapacity;
            if (newCap < DefaultCapacity)
                newCap = DefaultCapacity;

            // throw exception if newCapacity < NumItems
            if (newCap < numItems)
                throw new ArgumentOutOfRangeException("newCapacity", "New capacity is less than Count");

            this.capacity = newCap;
            if (items == null)
            {
                items = new PriorityQueueItem<TValue, TPriority>[newCap];
                return;
            }

            // Resize the array.
            Array.Resize(ref items, newCap);
        }

        public void Enqueue(PriorityQueueItem<TValue, TPriority> newItem)
        {
            if (numItems == capacity)
            {
                // need to increase capacity
                // grow by 50 percent
                SetCapacity((3*Capacity)/2);
            }

            int i = numItems;
            ++numItems;
            while ((i > 0) && (compareFunc(items[(i - 1)/2].Priority, newItem.Priority) < 0))
            {
                items[i] = items[(i - 1)/2];
                i = (i - 1)/2;
            }
            items[i] = newItem;
            //if (!VerifyQueue())
            //{
            //    Console.WriteLine("ERROR: Queue out of order!");
            //}
        }

        public void Enqueue(TValue value, TPriority priority)
        {
            Enqueue(new PriorityQueueItem<TValue, TPriority>(value, priority));
        }

        private PriorityQueueItem<TValue, TPriority> RemoveAt(Int32 index)
        {
            PriorityQueueItem<TValue, TPriority> o = items[index];
            --numItems;
            // move the last item to fill the hole
            PriorityQueueItem<TValue, TPriority> tmp = items[numItems];
            // If you forget to clear this, you have a potential memory leak.
            items[numItems] = default(PriorityQueueItem<TValue, TPriority>);
            if (numItems > 0 && index != numItems)
            {
                // If the new item is greater than its parent, bubble up.
                int i = index;
                int parent = (i - 1)/2;
                while (compareFunc(tmp.Priority, items[parent].Priority) > 0)
                {
                    items[i] = items[parent];
                    i = parent;
                    parent = (i - 1)/2;
                }

                // if i == index, then we didn't move the item up
                if (i == index)
                {
                    // bubble down ...
                    while (i < (numItems)/2)
                    {
                        int j = (2*i) + 1;
                        if ((j < numItems - 1) && (compareFunc(items[j].Priority, items[j + 1].Priority) < 0))
                        {
                            ++j;
                        }
                        if (compareFunc(items[j].Priority, tmp.Priority) <= 0)
                        {
                            break;
                        }
                        items[i] = items[j];
                        i = j;
                    }
                }
                // Be sure to store the item in its place.
                items[i] = tmp;
            }
            //if (!VerifyQueue())
            //{
            //    Console.WriteLine("ERROR: Queue out of order!");
            //}
            return o;
        }

        // Function to check that the queue is coherent.
        public bool VerifyQueue()
        {
            int i = 0;
            while (i < numItems/2)
            {
                int leftChild = (2*i) + 1;
                int rightChild = leftChild + 1;
                if (compareFunc(items[i].Priority, items[leftChild].Priority) < 0)
                {
                    return false;
                }
                if (rightChild < numItems && compareFunc(items[i].Priority, items[rightChild].Priority) < 0)
                {
                    return false;
                }
                ++i;
            }
            return true;
        }

        public PriorityQueueItem<TValue, TPriority> Dequeue()
        {
            if (Count == 0)
                throw new InvalidOperationException("The queue is empty");
            return RemoveAt(0);
        }

        public bool TryDequeue(out PriorityQueueItem<TValue, TPriority> value)
        {
            value = new PriorityQueueItem<TValue, TPriority>();
            if (Count == 0)
                return false;
            value = RemoveAt(0);
            return true;
        }

        /// <summary>
        ///   Removes the item with the specified value from the queue.
        ///   The passed equality comparison is used.
        /// </summary>
        /// <param name = "item">The item to be removed.</param>
        /// <param name = "comp">An object that implements the IEqualityComparer interface
        ///   for the type of item in the collection.</param>
        public void Remove(TValue item, IEqualityComparer comparer)
        {
            // need to find the PriorityQueueItem that has the Data value of o
            for (int index = 0; index < numItems; ++index)
            {
                if (comparer.Equals(item, items[index].Value))
                {
                    RemoveAt(index);
                    return;
                }
            }
            throw new ApplicationException("The specified itemm is not in the queue.");
        }

        /// <summary>
        ///   Removes the item with the specified value from the queue.
        ///   The passed equality comparison is used.
        /// </summary>
        /// <param name = "item">The item to be removed.</param>
        /// <param name = "comp">An object that implements the IEqualityComparer interface
        ///   for the type of item in the collection.</param>
        public TValue Find(TValue item, IComparer<TValue> comparer)
        {
            // need to find the PriorityQueueItem that has the Data value of o
            for (int index = 0; index < numItems; ++index)
            {
                if (comparer.Compare(item, items[index].Value) > 1)
                {
                    return items[index].Value;
                }
            }
            return default(TValue);
        }

        /// <summary>
        ///   Removes the item with the specified value from the queue.
        ///   The default type comparison function is used.
        /// </summary>
        /// <param name = "item">The item to be removed.</param>
        public void Remove(TValue item)
        {
            Remove(item, EqualityComparer<TValue>.Default);
        }

        public PriorityQueueItem<TValue, TPriority> Peek()
        {
            if (Count == 0)
                throw new InvalidOperationException("The queue is empty");
            return items[0];
        }

        // Clear
        public void Clear()
        {
            for (int i = 0; i < numItems; ++i)
            {
                items[i] = default(PriorityQueueItem<TValue, TPriority>);
            }
            numItems = 0;
            TrimExcess();
        }

        /// <summary>
        ///   Set the capacity to the actual number of items, if the current
        ///   number of items is less than 90 percent of the current capacity.
        /// </summary>
        public void TrimExcess()
        {
            if (numItems < (float) 0.9*capacity)
            {
                SetCapacity(numItems);
            }
        }

        // Contains
        public bool Contains(TValue o)
        {
#if (!ISWIN)
            foreach (PriorityQueueItem<TValue, TPriority> x in items)
            {
                if (x.Value.Equals(o)) return true;
            }
            return false;
#else
            return items.Any(x => x.Value.Equals(o));
#endif
        }

        public void CopyTo(PriorityQueueItem<TValue, TPriority>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex", "arrayIndex is less than 0.");
            if (array.Rank > 1)
                throw new ArgumentException("array is multidimensional.");
            if (numItems == 0)
                return;
            if (arrayIndex >= array.Length)
                throw new ArgumentException("arrayIndex is equal to or greater than the length of the array.");
            if (numItems > (array.Length - arrayIndex))
                throw new ArgumentException(
                    "The number of elements in the source ICollection is greater than the available space from arrayIndex to the end of the destination array.");

            for (int i = 0; i < numItems; i++)
            {
                array[arrayIndex + i] = items[i];
            }
        }
    }

    #region License

    /* Copyright (c) 2006 Leslie Sanford
     * 
     * Permission is hereby granted, free of charge, to any person obtaining a copy 
     * of this software and associated documentation files (the "Software"), to 
     * deal in the Software without restriction, including without limitation the 
     * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
     * sell copies of the Software, and to permit persons to whom the Software is 
     * furnished to do so, subject to the following conditions:
     * 
     * The above copyright notice and this permission notice shall be included in 
     * all copies or substantial portions of the Software. 
     * 
     * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
     * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
     * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
     * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
     * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
     * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
     * THE SOFTWARE.
     */

    #endregion

    #region Contact

    /*
     * Leslie Sanford
     * Email: jabberdabber@hotmail.com
     */

    #endregion

    /// <summary>
    ///   Represents the priority queue data structure.
    /// </summary>
    public class LPriorityQueue : ICollection
    {
        #region Fields

        // The maximum level of the skip list.
        private const int LevelMaxValue = 16;

        // The probability value used to randomly select the next level value.
        private const double Probability = 0.5;
        private readonly IComparer comparer;

        // The current level of the skip list.

        // Used to generate node levels.
        private readonly Random rand = new Random();

        // The number of elements in the PriorityQueue.
        private int count;
        private int currentLevel = 1;

        // The header node of the skip list.
        private Node header = new Node(null, LevelMaxValue);

        // The version of this PriorityQueue.
        private long version;

        // Used for comparing and sorting elements.

        #endregion

        #region Construction

        /// <summary>
        ///   Initializes a new instance of the PriorityQueue class.
        /// </summary>
        /// <remarks>
        ///   The PriorityQueue will cast its elements to the IComparable 
        ///   interface when making comparisons.
        /// </remarks>
        public LPriorityQueue()
        {
            comparer = new DefaultComparer();
        }

        /// <summary>
        ///   Initializes a new instance of the PriorityQueue class with the
        ///   specified IComparer.
        /// </summary>
        /// <param name = "comparer">
        ///   The IComparer to use for comparing and ordering elements.
        /// </param>
        /// <remarks>
        ///   If the specified IComparer is null, the PriorityQueue will cast its
        ///   elements to the IComparable interface when making comparisons.
        /// </remarks>
        public LPriorityQueue(IComparer comparer)
        {
            // If no comparer was provided.
            if (comparer == null)
            {
                // Use the DefaultComparer.
                this.comparer = new DefaultComparer();
            }
                // Else a comparer was provided.
            else
            {
                // Use the provided comparer.
                this.comparer = comparer;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///   Enqueues the specified element into the PriorityQueue.
        /// </summary>
        /// <param name = "element">
        ///   The element to enqueue into the PriorityQueue.
        /// </param>
        /// <exception cref = "ArgumentNullException">
        ///   If element is null.
        /// </exception>
        public virtual void Enqueue(object element)
        {
            #region Require

            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            #endregion

            Node x = header;
            Node[] update = new Node[LevelMaxValue];
            int nextLevel = NextLevel();

            // Find the place in the queue to insert the new element.
            for (int i = currentLevel - 1; i >= 0; i--)
            {
                while (x[i] != null && comparer.Compare(x[i].Element, element) > 0)
                {
                    x = x[i];
                }

                update[i] = x;
            }

            // If the new node's level is greater than the current level.
            if (nextLevel > currentLevel)
            {
                for (int i = currentLevel; i < nextLevel; i++)
                {
                    update[i] = header;
                }

                // Update level.
                currentLevel = nextLevel;
            }

            // Create new node.
            Node newNode = new Node(element, nextLevel);

            // Insert the new node into the list.
            for (int i = 0; i < nextLevel; i++)
            {
                newNode[i] = update[i][i];
                update[i][i] = newNode;
            }

            // Keep track of the number of elements in the PriorityQueue.
            count++;

            version++;
        }

        /// <summary>
        ///   Removes the element at the head of the PriorityQueue.
        /// </summary>
        /// <returns>
        ///   The element at the head of the PriorityQueue.
        /// </returns>
        /// <exception cref = "InvalidOperationException">
        ///   If Count is zero.
        /// </exception>
        public virtual object Dequeue()
        {
            #region Require

            if (Count == 0)
            {
                throw new InvalidOperationException(
                    "Cannot dequeue into an empty PriorityQueue.");
            }

            #endregion

            // Get the first item in the queue.
            object element = header[0].Element;

            // Keep track of the node that is about to be removed.
            Node oldNode = header[0];

            // Update the header so that its pointers that pointed to the
            // node to be removed now point to the node that comes after it.
            for (int i = 0; i < currentLevel && header[i] == oldNode; i++)
            {
                header[i] = oldNode[i];
            }

            // Update the current level of the list in case the node that
            // was removed had the highest level.
            while (currentLevel > 1 && header[currentLevel - 1] == null)
            {
                currentLevel--;
            }

            // Keep track of how many items are in the queue.
            count--;

            version++;

            return element;
        }

        /// <summary>
        ///   Removes the specified element from the PriorityQueue.
        /// </summary>
        /// <param name = "element">
        ///   The element to remove.
        /// </param>
        /// <exception cref = "ArgumentNullException">
        ///   If element is null
        /// </exception>
        public virtual void Remove(object element)
        {
            #region Require

            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            #endregion

            Node x = header;
            Node[] update = new Node[LevelMaxValue];

            // Find the specified element.
            for (int i = currentLevel - 1; i >= 0; i--)
            {
                while (x[i] != null && comparer.Compare(x[i].Element, element) > 0)
                {
                    x = x[i];
                }

                update[i] = x;
            }

            x = x[0];

            // If the specified element was found.
            if (x != null && comparer.Compare(x.Element, element) == 0)
            {
                // Remove element.
                for (int i = 0; i < currentLevel && update[i][i] == x; i++)
                {
                    update[i][i] = x[i];
                }

                // Update list level.
                while (currentLevel > 1 && header[currentLevel - 1] == null)
                {
                    currentLevel--;
                }

                // Keep track of the number of elements in the PriorityQueue.
                count--;

                version++;
            }
        }

        /// <summary>
        ///   Returns a value indicating whether the specified element is in the
        ///   PriorityQueue.
        /// </summary>
        /// <param name = "element">
        ///   The element to test.
        /// </param>
        /// <returns>
        ///   <b>true</b> if the element is in the PriorityQueue; otherwise
        ///   <b>false</b>.
        /// </returns>
        public virtual bool Contains(object element)
        {
            #region Guard

            if (element == null)
            {
                return false;
            }

            #endregion

            bool found;
            Node x = header;

            // Find the specified element.
            for (int i = currentLevel - 1; i >= 0; i--)
            {
                while (x[i] != null && comparer.Compare(x[i].Element, element) > 0)
                {
                    x = x[i];
                }
            }

            x = x[0];

            // If the element is in the PriorityQueue.
            if (x != null && comparer.Compare(x.Element, element) == 0)
            {
                found = true;
            }
                // Else the element is not in the PriorityQueue.
            else
            {
                found = false;
            }

            return found;
        }

        /// <summary>
        ///   Returns the element at the head of the PriorityQueue without 
        ///   removing it.
        /// </summary>
        /// <returns>
        ///   The element at the head of the PriorityQueue.
        /// </returns>
        public virtual object Peek()
        {
            #region Require

            if (Count == 0)
            {
                throw new InvalidOperationException(
                    "Cannot peek into an empty PriorityQueue.");
            }

            #endregion

            return header[0].Element;
        }

        /// <summary>
        ///   Removes all elements from the PriorityQueue.
        /// </summary>
        public virtual void Clear()
        {
            header = new Node(null, LevelMaxValue);

            currentLevel = 1;

            count = 0;

            version++;
        }

        /// <summary>
        ///   Returns a synchronized wrapper of the specified PriorityQueue.
        /// </summary>
        /// <param name = "queue">
        ///   The PriorityQueue to synchronize.
        /// </param>
        /// <returns>
        ///   A synchronized PriorityQueue.
        /// </returns>
        /// <exception cref = "ArgumentNullException">
        ///   If queue is null.
        /// </exception>
        public static LPriorityQueue Synchronized(LPriorityQueue queue)
        {
            #region Require

            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            #endregion

            return new SynchronizedPriorityQueue(queue);
        }

        // Generates a random level for the next node.
        private int NextLevel()
        {
            int nextLevel = 1;

            while (rand.NextDouble() < Probability &&
                   nextLevel < LevelMaxValue &&
                   nextLevel <= currentLevel)
            {
                nextLevel++;
            }

            return nextLevel;
        }

        #endregion

        #region Private Classes

        #region SynchronizedPriorityQueue Class

        // A synchronized wrapper for the PriorityQueue class.
        private class SynchronizedPriorityQueue : LPriorityQueue
        {
            private readonly LPriorityQueue queue;

            private readonly object root;

            public SynchronizedPriorityQueue(LPriorityQueue queue)
            {
                #region Require

                if (queue == null)
                {
                    throw new ArgumentNullException("queue");
                }

                #endregion

                this.queue = queue;

                root = queue.SyncRoot;
            }

            public override int Count
            {
                get
                {
                    lock (root)
                    {
                        return queue.Count;
                    }
                }
            }

            public override bool IsSynchronized
            {
                get { return true; }
            }

            public override object SyncRoot
            {
                get { return root; }
            }

            public override void Enqueue(object element)
            {
                lock (root)
                {
                    queue.Enqueue(element);
                }
            }

            public override object Dequeue()
            {
                lock (root)
                {
                    return queue.Dequeue();
                }
            }

            public override void Remove(object element)
            {
                lock (root)
                {
                    queue.Remove(element);
                }
            }

            public override void Clear()
            {
                lock (root)
                {
                    queue.Clear();
                }
            }

            public override bool Contains(object element)
            {
                lock (root)
                {
                    return queue.Contains(element);
                }
            }

            public override object Peek()
            {
                lock (root)
                {
                    return queue.Peek();
                }
            }

            public override void CopyTo(Array array, int index)
            {
                lock (root)
                {
                    queue.CopyTo(array, index);
                }
            }

            public override IEnumerator GetEnumerator()
            {
                lock (root)
                {
                    return queue.GetEnumerator();
                }
            }
        }

        #endregion

        #region DefaultComparer Class

        // The IComparer to use of no comparer was provided.
        private class DefaultComparer : IComparer
        {
            #region IComparer Members

            public int Compare(object x, object y)
            {
                #region Require

                if (!(y is IComparable))
                {
                    throw new ArgumentException(
                        "Item does not implement IComparable.");
                }

                #endregion

                IComparable a = x as IComparable;

                return a.CompareTo(y);
            }

            #endregion
        }

        #endregion

        #region Node Class

        // Represents a node in the list of nodes.
        private class Node
        {
            private readonly object element;
            private readonly Node[] forward;

            public Node(object element, int level)
            {
                this.forward = new Node[level];
                this.element = element;
            }

            public Node this[int index]
            {
                get { return forward[index]; }
                set { forward[index] = value; }
            }

            public object Element
            {
                get { return element; }
            }
        }

        #endregion

        #region PriorityQueueEnumerator Class

        // Implements the IEnumerator interface for the PriorityQueue class.
        private class PriorityQueueEnumerator : IEnumerator
        {
            private readonly Node head;
            private readonly LPriorityQueue owner;
            private readonly long version;

            private Node currentNode;

            private bool moveResult;

            public PriorityQueueEnumerator(LPriorityQueue owner)
            {
                this.owner = owner;
                this.version = owner.version;
                head = owner.header;

                Reset();
            }

            #region IEnumerator Members

            public void Reset()
            {
                #region Require

                if (version != owner.version)
                {
                    throw new InvalidOperationException(
                        "The PriorityQueue was modified after the enumerator was created.");
                }

                #endregion

                currentNode = head;
                moveResult = true;
            }

            public object Current
            {
                get
                {
                    #region Require

                    if (currentNode == head || currentNode == null)
                    {
                        throw new InvalidOperationException(
                            "The enumerator is positioned before the first " +
                            "element of the collection or after the last element.");
                    }

                    #endregion

                    return currentNode.Element;
                }
            }

            public bool MoveNext()
            {
                #region Require

                if (version != owner.version)
                {
                    throw new InvalidOperationException(
                        "The PriorityQueue was modified after the enumerator was created.");
                }

                #endregion

                if (moveResult)
                {
                    currentNode = currentNode[0];
                }

                if (currentNode == null)
                {
                    moveResult = false;
                }

                return moveResult;
            }

            #endregion
        }

        #endregion

        #endregion

        #region ICollection Members

        public virtual bool IsSynchronized
        {
            get { return false; }
        }

        public virtual int Count
        {
            get { return count; }
        }

        public virtual void CopyTo(Array array, int index)
        {
            #region Require

            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            else if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index", index,
                                                      "Array index out of range.");
            }
            else if (array.Rank > 1)
            {
                throw new ArgumentException(
                    "Array has more than one dimension.", "array");
            }
            else if (index >= array.Length)
            {
                throw new ArgumentException(
                    "index is equal to or greater than the length of array.", "index");
            }
            else if (Count > array.Length - index)
            {
                throw new ArgumentException(
                    "The number of elements in the PriorityQueue is greater " +
                    "than the available space from index to the end of the " +
                    "destination array.", "index");
            }

            #endregion

            int i = index;

            foreach (object element in this)
            {
                array.SetValue(element, i);
                i++;
            }
        }

        public virtual object SyncRoot
        {
            get { return this; }
        }

        public virtual IEnumerator GetEnumerator()
        {
            return new PriorityQueueEnumerator(this);
        }

        #endregion
    }
}