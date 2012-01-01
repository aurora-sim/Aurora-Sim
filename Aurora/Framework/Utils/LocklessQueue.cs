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

using System.Collections.Generic;
using System.Threading;

namespace Aurora.Framework
{

    #region LockFreeQueue from http://www.boyet.com/Articles/LockfreeStack.html. Thanks to Julian M. Bucknall.

    internal class SingleLinkNode<T>
    {
        // Note; the Next member cannot be a property since it participates in
        // many CAS operations
        public T Item;
        public SingleLinkNode<T> Next;
    }

    public interface IPriorityConverter<P>
    {
        int PriorityCount { get; }
        int Convert(P priority);
    }

    public class LimitedPriorityQueue<T, P>
    {
        private readonly IPriorityConverter<P> converter;
        private readonly LockFreeQueue<T>[] queueList;

        public LimitedPriorityQueue(IPriorityConverter<P> converter)
        {
            this.converter = converter;
            this.queueList = new LockFreeQueue<T>[converter.PriorityCount];
            for (int i = 0; i < queueList.Length; i++)
            {
                queueList[i] = new LockFreeQueue<T>();
            }
        }

        public void Enqueue(T item, P priority)
        {
            this.queueList[converter.Convert(priority)].Enqueue(item);
        }

        public bool Dequeue(out T item)
        {
            foreach (LockFreeQueue<T> q in queueList)
            {
                if (q.Dequeue(out item))
                {
                    return true;
                }
            }
            item = default(T);
            return false;
        }

        public T Dequeue()
        {
            T result;
            Dequeue(out result);
            return result;
        }
    }

    public class LockFreeQueue<T>
    {
        private SingleLinkNode<T> head;
        private SingleLinkNode<T> tail;

        public LockFreeQueue()
        {
            head = new SingleLinkNode<T>();
            tail = head;
        }

        public void Enqueue(T item)
        {
            SingleLinkNode<T> oldTail = null;
            SingleLinkNode<T> oldTailNext;

            SingleLinkNode<T> newNode = new SingleLinkNode<T> {Item = item};

            bool newNodeWasAdded = false;
            while (!newNodeWasAdded)
            {
                oldTail = tail;
                oldTailNext = oldTail.Next;

                if (tail == oldTail)
                {
                    if (oldTailNext == null)
                        newNodeWasAdded = SyncMethods.CAS(ref tail.Next, null, newNode);
                    else
                        SyncMethods.CAS(ref tail, oldTail, oldTailNext);
                }
            }

            SyncMethods.CAS(ref tail, oldTail, newNode);
        }

        public bool Dequeue(out T item)
        {
            item = default(T);
            SingleLinkNode<T> oldHead = null;

            bool haveAdvancedHead = false;
            while (!haveAdvancedHead)
            {
                oldHead = head;
                SingleLinkNode<T> oldTail = tail;
                SingleLinkNode<T> oldHeadNext = oldHead.Next;

                if (oldHead == head)
                {
                    if (oldHead == oldTail)
                    {
                        if (oldHeadNext == null)
                        {
                            return false;
                        }
                        SyncMethods.CAS(ref tail, oldTail, oldHeadNext);
                    }

                    else
                    {
                        item = oldHeadNext.Item;
                        haveAdvancedHead =
                            SyncMethods.CAS(ref head, oldHead, oldHeadNext);
                    }
                }
            }
            return true;
        }

        public T Dequeue()
        {
            T result;
            Dequeue(out result);
            return result;
        }
    }

    public static class SyncMethods
    {
        public static bool CAS<T>(ref T location, T comparand, T newValue) where T : class
        {
            return
                comparand ==
                Interlocked.CompareExchange(ref location, newValue, comparand);
        }
    }

    #endregion

    public sealed class LocklessQueue<T>
    {
        private int count;
        private SingleLinkNode head;
        private SingleLinkNode tail;

        public LocklessQueue()
        {
            Init();
        }

        public int Count
        {
            get { return count; }
        }

        public void Enqueue(T item)
        {
            SingleLinkNode oldTail = null;
            SingleLinkNode oldTailNext;

            SingleLinkNode newNode = new SingleLinkNode {Item = item};

            bool newNodeWasAdded = false;

            while (!newNodeWasAdded)
            {
                oldTail = tail;
                oldTailNext = oldTail.Next;

                if (tail == oldTail)
                {
                    if (oldTailNext == null)
                        newNodeWasAdded = CAS(ref tail.Next, null, newNode);
                    else
                        CAS(ref tail, oldTail, oldTailNext);
                }
            }

            CAS(ref tail, oldTail, newNode);
            Interlocked.Increment(ref count);
        }

        public bool Dequeue(out T item)
        {
            item = default(T);
            SingleLinkNode oldHead = null;
            bool haveAdvancedHead = false;

            while (!haveAdvancedHead)
            {
                oldHead = head;
                SingleLinkNode oldTail = tail;
                SingleLinkNode oldHeadNext = oldHead.Next;

                if (oldHead == head)
                {
                    if (oldHead == oldTail)
                    {
                        if (oldHeadNext == null)
                            return false;

                        CAS(ref tail, oldTail, oldHeadNext);
                    }
                    else
                    {
                        item = oldHeadNext.Item;
                        haveAdvancedHead = CAS(ref head, oldHead, oldHeadNext);
                    }
                }
            }

            Interlocked.Decrement(ref count);
            return true;
        }

        public bool Dequeue(int num, out List<T> items)
        {
            items = new List<T>(num);
            SingleLinkNode oldHead = null;
            bool haveAdvancedHead = false;

            for (int i = 0; i < num; i++)
            {
                while (!haveAdvancedHead)
                {
                    oldHead = head;
                    SingleLinkNode oldTail = tail;
                    SingleLinkNode oldHeadNext = oldHead.Next;

                    if (oldHead == head)
                    {
                        if (oldHead == oldTail)
                        {
                            if (oldHeadNext == null)
                                return false;

                            CAS(ref tail, oldTail, oldHeadNext);
                        }
                        else
                        {
                            items.Add(oldHeadNext.Item);
                            haveAdvancedHead = CAS(ref head, oldHead, oldHeadNext);
                        }
                    }
                }
            }

            Interlocked.Decrement(ref count);
            return true;
        }

        public void Clear()
        {
            Init();
        }

        private void Init()
        {
            count = 0;
            head = tail = new SingleLinkNode();
        }

        private static bool CAS(ref SingleLinkNode location, SingleLinkNode comparand, SingleLinkNode newValue)
        {
            return
                comparand ==
                Interlocked.CompareExchange(ref location, newValue, comparand);
        }

        #region Nested type: SingleLinkNode

        private sealed class SingleLinkNode
        {
            public T Item;
            public SingleLinkNode Next;
        }

        #endregion
    }
}