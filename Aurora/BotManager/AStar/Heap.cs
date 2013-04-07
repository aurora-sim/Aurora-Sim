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

using System;
using System.Collections;

namespace Tanis.Collections
{
    /// <summary>
    ///     The Heap allows to maintain a list sorted as long as needed.
    ///     If no IComparer interface has been provided at construction, then the list expects the Objects to implement IComparer.
    ///     If the list is not sorted it behaves like an ordinary list.
    ///     When sorted, the list's "Add" method will put new objects at the right place.
    ///     As well the "Contains" and "IndexOf" methods will perform a binary search.
    /// </summary>
    public class Heap : IList, ICloneable
    {
        #region Delegates

        /// <summary>
        ///     Defines an equality for two objects
        /// </summary>
        public delegate bool Equality(object Object1, object Object2);

        #endregion

        private IComparer FComparer;
        private ArrayList FList;
        private bool FUseObjectsComparison;

        #region Constructors

        /// <summary>
        ///     Default constructor.
        ///     Since no IComparer is provided here, added objects must implement the IComparer interface.
        /// </summary>
        public Heap()
        {
            InitProperties(null, 0);
        }

        /// <summary>
        ///     Constructor.
        ///     Since no IComparer is provided, added objects must implement the IComparer interface.
        /// </summary>
        /// <param name="Capacity">
        ///     Capacity of the list (<see cref="ArrayList.Capacity">ArrayList.Capacity</see>)
        /// </param>
        public Heap(int Capacity)
        {
            InitProperties(null, Capacity);
        }

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="Comparer">Will be used to compare added elements for sort and search operations.</param>
        public Heap(IComparer Comparer)
        {
            InitProperties(Comparer, 0);
        }

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="Comparer">Will be used to compare added elements for sort and search operations.</param>
        /// <param name="Capacity">
        ///     Capacity of the list (<see cref="ArrayList.Capacity">ArrayList.Capacity</see>)
        /// </param>
        public Heap(IComparer Comparer, int Capacity)
        {
            InitProperties(Comparer, Capacity);
        }

        #endregion

        #region Properties

        private bool FAddDuplicates;

        /// <summary>
        ///     If set to true, it will not be possible to add an object to the list if its value is already in the list.
        /// </summary>
        public bool AddDuplicates
        {
            set { FAddDuplicates = value; }
            get { return FAddDuplicates; }
        }

        /// <summary>
        ///     Idem <see cref="ArrayList">ArrayList</see>
        /// </summary>
        public int Capacity
        {
            get { return FList.Capacity; }
            set { FList.Capacity = value; }
        }

        #endregion

        #region ICloneable Members

        /// <summary>
        ///     ICloneable implementation.
        ///     Idem <see cref="ArrayList">ArrayList</see>
        /// </summary>
        /// <returns>Cloned object.</returns>
        public object Clone()
        {
            Heap Clone = new Heap(FComparer, FList.Capacity)
                             {FList = (ArrayList) FList.Clone(), FAddDuplicates = FAddDuplicates};
            return Clone;
        }

        #endregion

        #region IList Members

        /// <summary>
        ///     IList implementation.
        ///     Gets object's value at a specified index.
        ///     The set operation is impossible on a Heap.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Index is less than zero or Index is greater than Count.</exception>
        /// <exception cref="InvalidOperationException">[] operator cannot be used to set a value on a Heap.</exception>
        public object this[int Index]
        {
            get
            {
                if (Index >= FList.Count || Index < 0)
                    throw new ArgumentOutOfRangeException("Index is less than zero or Index is greater than Count.");
                return FList[Index];
            }
            set { throw new InvalidOperationException("[] operator cannot be used to set a value in a Heap."); }
        }

        /// <summary>
        ///     IList implementation.
        ///     Adds the object at the right place.
        /// </summary>
        /// <param name="O">The object to add.</param>
        /// <returns>The index where the object has been added.</returns>
        /// <exception cref="ArgumentException">The Heap is set to use object's IComparable interface, and the specifed object does not implement this interface.</exception>
        public int Add(object O)
        {
            int Return = -1;
            if (ObjectIsCompliant(O))
            {
                int Index = IndexOf(O);
                int NewIndex = Index >= 0 ? Index : -Index - 1;
                if (NewIndex >= Count) FList.Add(O);
                else FList.Insert(NewIndex, O);
                Return = NewIndex;
            }
            return Return;
        }

        /// <summary>
        ///     IList implementation.
        ///     Search for a specified object in the list.
        ///     If the list is sorted, a &lt;see cref = &quot;ArrayList.BinarySearch&quot;&gt;BinarySearch&lt;/see&gt; is performed using IComparer interface.
        ///     Else the &lt;see cref = &quot;Equals&quot;&gt;Object.Equals&lt;/see&gt; implementation is used.
        /// </summary>
        /// <param name="O">The object to look for</param>
        /// <returns>true if the object is in the list, otherwise false.</returns>
        public bool Contains(object O)
        {
            return FList.BinarySearch(O, FComparer) >= 0;
        }

        /// <summary>
        ///     IList implementation.
        ///     Returns the index of the specified object in the list.
        ///     If the list is sorted, a &lt;see cref = &quot;ArrayList.BinarySearch&quot;&gt;BinarySearch&lt;/see&gt; is performed using IComparer interface.
        ///     Else the &lt;see cref = &quot;Equals&quot;&gt;Object.Equals&lt;/see&gt; implementation of objects is used.
        /// </summary>
        /// <param name="O">The object to locate.</param>
        /// <returns>
        ///     If the object has been found, a positive integer corresponding to its position.
        ///     If the objects has not been found, a negative integer which is the bitwise complement of the index of the next element.
        /// </returns>
        public int IndexOf(object O)
        {
            int Result = -1;
            Result = FList.BinarySearch(O, FComparer);
            while (Result > 0 && FList[Result - 1].Equals(O))
                Result--;
            return Result;
        }

        /// <summary>
        ///     IList implementation.
        ///     Idem <see cref="ArrayList">ArrayList</see>
        /// </summary>
        public bool IsFixedSize
        {
            get { return FList.IsFixedSize; }
        }

        /// <summary>
        ///     IList implementation.
        ///     Idem <see cref="ArrayList">ArrayList</see>
        /// </summary>
        public bool IsReadOnly
        {
            get { return FList.IsReadOnly; }
        }

        /// <summary>
        ///     IList implementation.
        ///     Idem <see cref="ArrayList">ArrayList</see>
        /// </summary>
        public void Clear()
        {
            FList.Clear();
        }

        /// <summary>
        ///     IList implementation.
        ///     Cannot be used on a Heap.
        /// </summary>
        /// <param name="Index">The index before which the object must be added.</param>
        /// <param name="O">The object to add.</param>
        /// <exception cref="InvalidOperationException">Insert method cannot be called on a Heap.</exception>
        public void Insert(int Index, object O)
        {
            throw new InvalidOperationException("Insert method cannot be called on a Heap.");
        }

        /// <summary>
        ///     IList implementation.
        ///     Idem <see cref="ArrayList">ArrayList</see>
        /// </summary>
        /// <param name="Value">The object whose value must be removed if found in the list.</param>
        public void Remove(object Value)
        {
            FList.Remove(Value);
        }

        /// <summary>
        ///     IList implementation.
        ///     Idem <see cref="ArrayList">ArrayList</see>
        /// </summary>
        /// <param name="Index">Index of object to remove.</param>
        public void RemoveAt(int Index)
        {
            FList.RemoveAt(Index);
        }

        /// <summary>
        ///     IList.ICollection implementation.
        ///     Idem <see cref="ArrayList">ArrayList</see>
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(Array array, int arrayIndex)
        {
            FList.CopyTo(array, arrayIndex);
        }

        /// <summary>
        ///     IList.ICollection implementation.
        ///     Idem <see cref="ArrayList">ArrayList</see>
        /// </summary>
        public int Count
        {
            get { return FList.Count; }
        }

        /// <summary>
        ///     IList.ICollection implementation.
        ///     Idem <see cref="ArrayList">ArrayList</see>
        /// </summary>
        public bool IsSynchronized
        {
            get { return FList.IsSynchronized; }
        }

        /// <summary>
        ///     IList.ICollection implementation.
        ///     Idem <see cref="ArrayList">ArrayList</see>
        /// </summary>
        public object SyncRoot
        {
            get { return FList.SyncRoot; }
        }

        /// <summary>
        ///     IList.IEnumerable implementation.
        ///     Idem <see cref="ArrayList">ArrayList</see>
        /// </summary>
        /// <returns>Enumerator on the list.</returns>
        public IEnumerator GetEnumerator()
        {
            return FList.GetEnumerator();
        }

        #endregion

        /// <summary>
        ///     Object.ToString() override.
        ///     Build a string to represent the list.
        /// </summary>
        /// <returns>The string refecting the list.</returns>
        public override string ToString()
        {
            string OutString = "{";
            for (int i = 0; i < FList.Count; i++)
                OutString += FList[i] + (i != FList.Count - 1 ? "; " : "}");
            return OutString;
        }

        /// <summary>
        ///     Object.Equals() override.
        /// </summary>
        /// <returns>true if object is equal to this, otherwise false.</returns>
        public override bool Equals(object Object)
        {
            Heap SL = (Heap) Object;
            if (SL.Count != Count)
                return false;
            for (int i = 0; i < Count; i++)
                if (!SL[i].Equals(this[i]))
                    return false;
            return true;
        }

        /// <summary>
        ///     Object.GetHashCode() override.
        /// </summary>
        /// <returns>Hash code for this.</returns>
        public override int GetHashCode()
        {
            return FList.GetHashCode();
        }

        /// <summary>
        ///     Idem IndexOf(object), but starting at a specified position in the list
        /// </summary>
        /// <param name="Object">The object to locate.</param>
        /// <param name="Start">The index for start position.</param>
        /// <returns></returns>
        public int IndexOf(object Object, int Start)
        {
            int Result = -1;
            Result = FList.BinarySearch(Start, FList.Count - Start, Object, FComparer);
            while (Result > Start && FList[Result - 1].Equals(Object))
                Result--;
            return Result;
        }

        /// <summary>
        ///     Idem IndexOf(object), but with a specified equality function
        /// </summary>
        /// <param name="Object">The object to locate.</param>
        /// <param name="AreEqual">Equality function to use for the search.</param>
        /// <returns></returns>
        public int IndexOf(object Object, Equality AreEqual)
        {
            for (int i = 0; i < FList.Count; i++)
                if (AreEqual(FList[i], Object)) return i;
            return -1;
        }

        /// <summary>
        ///     Idem IndexOf(object), but with a start index and a specified equality function
        /// </summary>
        /// <param name="Object">The object to locate.</param>
        /// <param name="Start">The index for start position.</param>
        /// <param name="AreEqual">Equality function to use for the search.</param>
        /// <returns></returns>
        public int IndexOf(object Object, int Start, Equality AreEqual)
        {
            if (Start < 0 || Start >= FList.Count)
                throw new ArgumentException("Start index must belong to [0; Count-1].");
            for (int i = Start; i < FList.Count; i++)
                if (AreEqual(FList[i], Object)) return i;
            return -1;
        }

        /// <summary>
        ///     The objects will be added at the right place.
        /// </summary>
        /// <param name="C">The object to add.</param>
        /// <returns>The index where the object has been added.</returns>
        /// <exception cref="ArgumentException">The Heap is set to use object's IComparable interface, and the specifed object does not implement this interface.</exception>
        public void AddRange(ICollection C)
        {
            foreach (object Object in C)
                Add(Object);
        }

        /// <summary>
        ///     Cannot be called on a Heap.
        /// </summary>
        /// <param name="Index">The index before which the objects must be added.</param>
        /// <param name="C">The object to add.</param>
        /// <exception cref="InvalidOperationException">Insert cannot be called on a Heap.</exception>
        public void InsertRange(int Index, ICollection C)
        {
            throw new InvalidOperationException("Insert cannot be called on a Heap.");
        }

        /// <summary>
        ///     Limits the number of occurrences of a specified value.
        ///     Same values are equals according to the Equals() method of objects in the list.
        ///     The first occurrences encountered are kept.
        /// </summary>
        /// <param name="Value">Value whose occurrences number must be limited.</param>
        /// <param name="NumberToKeep">Number of occurrences to keep</param>
        public void LimitOccurrences(object Value, int NumberToKeep)
        {
            if (Value == null)
                throw new ArgumentNullException("Value");
            int Pos = 0;
            while ((Pos = IndexOf(Value, Pos)) >= 0)
            {
                if (NumberToKeep <= 0)
                    FList.RemoveAt(Pos);
                else
                {
                    Pos++;
                    NumberToKeep--;
                }
                if (FComparer.Compare(FList[Pos], Value) > 0)
                    break;
            }
        }

        /// <summary>
        ///     Removes all duplicates in the list.
        ///     Each value encountered will have only one representant.
        /// </summary>
        public void RemoveDuplicates()
        {
            int PosIt;
            PosIt = 0;
            while (PosIt < Count - 1)
            {
                if (FComparer.Compare(this[PosIt], this[PosIt + 1]) == 0)
                    RemoveAt(PosIt);
                else
                    PosIt++;
            }
        }

        /// <summary>
        ///     Returns the object of the list whose value is minimum
        /// </summary>
        /// <returns>The minimum object in the list</returns>
        public int IndexOfMin()
        {
            int RetInt = -1;
            if (FList.Count > 0)
                RetInt = 0;
            return RetInt;
        }

        /// <summary>
        ///     Returns the object of the list whose value is maximum
        /// </summary>
        /// <returns>The maximum object in the list</returns>
        public int IndexOfMax()
        {
            int RetInt = -1;
            if (FList.Count > 0)
            {
                RetInt = FList.Count - 1;
            }
            return RetInt;
        }

        /// <summary>
        ///     Returns the topmost object on the list and removes it from the list
        /// </summary>
        /// <returns>Returns the topmost object on the list</returns>
        public object Pop()
        {
            if (FList.Count == 0)
                throw new InvalidOperationException("The heap is empty.");
            object Object = FList[Count - 1];
            FList.RemoveAt(Count - 1);
            return (Object);
        }

        /// <summary>
        ///     Pushes an object on list. It will be inserted at the right spot.
        /// </summary>
        /// <param name="Object">Object to add to the list</param>
        /// <returns></returns>
        public int Push(object Object)
        {
            return (Add(Object));
        }

        private bool ObjectIsCompliant(object Object)
        {
            if (FUseObjectsComparison && !(Object is IComparable))
                throw new ArgumentException(
                    "The Heap is set to use the IComparable interface of objects, and the object to add does not implement the IComparable interface.");
            if (!FAddDuplicates && Contains(Object))
                return false;
            return true;
        }

        private void InitProperties(IComparer Comparer, int Capacity)
        {
            if (Comparer != null)
            {
                FComparer = Comparer;
                FUseObjectsComparison = false;
            }
            else
            {
                FComparer = new Comparison();
                FUseObjectsComparison = true;
            }
            FList = Capacity > 0 ? new ArrayList(Capacity) : new ArrayList();
            FAddDuplicates = true;
        }

        #region Nested type: Comparison

        private class Comparison : IComparer
        {
            #region IComparer Members

            public int Compare(object Object1, object Object2)
            {
                IComparable C = Object1 as IComparable;
                return C.CompareTo(Object2);
            }

            #endregion
        }

        #endregion
    }
}