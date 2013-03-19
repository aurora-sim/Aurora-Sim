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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Aurora.Framework
{

    #region TimedCacheKey Class

    internal class TimedCacheKey<TKey> : IComparable<TKey>
    {
        private readonly TKey key;
        private readonly bool slidingExpiration;
        private readonly TimeSpan slidingExpirationWindowSize;
        private DateTime expirationDate;

        public TimedCacheKey(TKey key, DateTime expirationDate)
        {
            this.key = key;
            this.slidingExpiration = false;
            this.expirationDate = expirationDate;
        }

        public TimedCacheKey(TKey key, TimeSpan slidingExpirationWindowSize)
        {
            this.key = key;
            this.slidingExpiration = true;
            this.slidingExpirationWindowSize = slidingExpirationWindowSize;
            Accessed();
        }

        public DateTime ExpirationDate
        {
            get { return expirationDate; }
        }

        public TKey Key
        {
            get { return key; }
        }

        public bool SlidingExpiration
        {
            get { return slidingExpiration; }
        }

        public TimeSpan SlidingExpirationWindowSize
        {
            get { return slidingExpirationWindowSize; }
        }

        #region IComparable<TKey> Members

        public int CompareTo(TKey other)
        {
            return key.GetHashCode().CompareTo(other.GetHashCode());
        }

        #endregion

        public void Accessed()
        {
            if (slidingExpiration)
                expirationDate = DateTime.Now.Add(slidingExpirationWindowSize);
        }
    }

    #endregion

    /// <summary>
    ///     List that has an expiring built in
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public sealed class ExpiringList<TKey>
    {
        private const double CACHE_PURGE_HZ = 1.0;
        private const int MAX_LOCK_WAIT = 5000; // milliseconds

        #region Private fields

        /// <summary>
        ///     For thread safety
        /// </summary>
        private readonly object isPurging = new object();

        /// <summary>
        ///     For thread safety
        /// </summary>
        private readonly object syncRoot = new object();

        private readonly List<TimedCacheKey<TKey>> timedStorage = new List<TimedCacheKey<TKey>>();

        private readonly Dictionary<TKey, TimedCacheKey<TKey>> timedStorageIndex =
            new Dictionary<TKey, TimedCacheKey<TKey>>();

        private readonly Timer timer = new Timer(TimeSpan.FromSeconds(CACHE_PURGE_HZ).TotalMilliseconds);
        private double DefaultTime;

        #endregion

        #region Constructor

        public ExpiringList()
        {
            timer.Elapsed += PurgeCache;
            timer.Start();
        }

        #endregion

        #region Public methods

        public TKey this[int i]
        {
            get
            {
                TKey o;
                if (!Monitor.TryEnter(syncRoot, MAX_LOCK_WAIT))
                    throw new ApplicationException("Lock could not be acquired after " + MAX_LOCK_WAIT + "ms");
                try
                {
                    if (timedStorage.Count > i)
                    {
                        TimedCacheKey<TKey> tkey = timedStorage[i];
                        o = tkey.Key;
                        timedStorage.Remove(tkey);
                        tkey.Accessed();
                        timedStorage.Insert(i, tkey);
                        return o;
                    }
                    else
                    {
                        throw new ArgumentException("Key not found in the cache");
                    }
                }
                finally
                {
                    Monitor.Exit(syncRoot);
                }
            }
            set { AddOrUpdate(value, DefaultTime); }
        }

        public int Count
        {
            get { return timedStorage.Count; }
        }

        public void SetDefaultTime(double time)
        {
            DefaultTime = time;
        }

        public bool Add(TKey key, double expirationSeconds)
        {
            if (!Monitor.TryEnter(syncRoot, MAX_LOCK_WAIT))
                throw new ApplicationException("Lock could not be acquired after " + MAX_LOCK_WAIT + "ms");
            try
            {
                // This is the actual adding of the key
                if (timedStorageIndex.ContainsKey(key))
                {
                    return false;
                }
                else
                {
                    TimedCacheKey<TKey> internalKey = new TimedCacheKey<TKey>(key,
                                                                              DateTime.UtcNow +
                                                                              TimeSpan.FromSeconds(expirationSeconds));
                    timedStorage.Add(internalKey);
                    timedStorageIndex.Add(key, internalKey);
                    return true;
                }
            }
            finally
            {
                Monitor.Exit(syncRoot);
            }
        }

        public bool Add(TKey key, TimeSpan slidingExpiration)
        {
            if (!Monitor.TryEnter(syncRoot, MAX_LOCK_WAIT))
                throw new ApplicationException("Lock could not be acquired after " + MAX_LOCK_WAIT + "ms");
            try
            {
                // This is the actual adding of the key
                if (timedStorageIndex.ContainsKey(key))
                {
                    return false;
                }
                else
                {
                    TimedCacheKey<TKey> internalKey = new TimedCacheKey<TKey>(key, slidingExpiration);
                    timedStorage.Add(internalKey);
                    timedStorageIndex.Add(key, internalKey);
                    return true;
                }
            }
            finally
            {
                Monitor.Exit(syncRoot);
            }
        }

        public bool AddOrUpdate(TKey key, double expirationSeconds)
        {
            if (!Monitor.TryEnter(syncRoot, MAX_LOCK_WAIT))
                throw new ApplicationException("Lock could not be acquired after " + MAX_LOCK_WAIT + "ms");
            try
            {
                if (Contains(key))
                {
                    Update(key, expirationSeconds);
                    return false;
                }
                else
                {
                    Add(key, expirationSeconds);
                    return true;
                }
            }
            finally
            {
                Monitor.Exit(syncRoot);
            }
        }

        public bool AddOrUpdate(TKey key, TimeSpan slidingExpiration)
        {
            if (!Monitor.TryEnter(syncRoot, MAX_LOCK_WAIT))
                throw new ApplicationException("Lock could not be acquired after " + MAX_LOCK_WAIT + "ms");
            try
            {
                if (Contains(key))
                {
                    Update(key, slidingExpiration);
                    return false;
                }
                else
                {
                    Add(key, slidingExpiration);
                    return true;
                }
            }
            finally
            {
                Monitor.Exit(syncRoot);
            }
        }

        public void Clear()
        {
            if (!Monitor.TryEnter(syncRoot, MAX_LOCK_WAIT))
                throw new ApplicationException("Lock could not be acquired after " + MAX_LOCK_WAIT + "ms");
            try
            {
                timedStorage.Clear();
                timedStorageIndex.Clear();
            }
            finally
            {
                Monitor.Exit(syncRoot);
            }
        }

        public bool Contains(TKey key)
        {
            if (!Monitor.TryEnter(syncRoot, MAX_LOCK_WAIT))
                throw new ApplicationException("Lock could not be acquired after " + MAX_LOCK_WAIT + "ms");
            try
            {
                return timedStorageIndex.ContainsKey(key);
            }
            finally
            {
                Monitor.Exit(syncRoot);
            }
        }

        public bool Remove(TKey key)
        {
            if (!Monitor.TryEnter(syncRoot, MAX_LOCK_WAIT))
                throw new ApplicationException("Lock could not be acquired after " + MAX_LOCK_WAIT + "ms");
            try
            {
                if (timedStorageIndex.ContainsKey(key))
                {
                    timedStorage.Remove(timedStorageIndex[key]);
                    timedStorageIndex.Remove(key);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                Monitor.Exit(syncRoot);
            }
        }

        public bool Update(TKey key)
        {
            if (!Monitor.TryEnter(syncRoot, MAX_LOCK_WAIT))
                throw new ApplicationException("Lock could not be acquired after " + MAX_LOCK_WAIT + "ms");
            try
            {
                if (timedStorageIndex.ContainsKey(key))
                {
                    timedStorage.Remove(timedStorageIndex[key]);
                    timedStorageIndex[key].Accessed();
                    timedStorage.Add(timedStorageIndex[key]);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                Monitor.Exit(syncRoot);
            }
        }

        public bool Update(TKey key, double expirationSeconds)
        {
            if (!Monitor.TryEnter(syncRoot, MAX_LOCK_WAIT))
                throw new ApplicationException("Lock could not be acquired after " + MAX_LOCK_WAIT + "ms");
            try
            {
                if (timedStorageIndex.ContainsKey(key))
                {
                    timedStorage.Remove(timedStorageIndex[key]);
                    timedStorageIndex.Remove(key);
                }
                else
                {
                    return false;
                }

                TimedCacheKey<TKey> internalKey = new TimedCacheKey<TKey>(key,
                                                                          DateTime.UtcNow +
                                                                          TimeSpan.FromSeconds(expirationSeconds));
                timedStorage.Add(internalKey);
                timedStorageIndex.Add(key, internalKey);
                return true;
            }
            finally
            {
                Monitor.Exit(syncRoot);
            }
        }

        public bool Update(TKey key, TimeSpan slidingExpiration)
        {
            if (!Monitor.TryEnter(syncRoot, MAX_LOCK_WAIT))
                throw new ApplicationException("Lock could not be acquired after " + MAX_LOCK_WAIT + "ms");
            try
            {
                if (timedStorageIndex.ContainsKey(key))
                {
                    timedStorage.Remove(timedStorageIndex[key]);
                    timedStorageIndex.Remove(key);
                }
                else
                {
                    return false;
                }

                TimedCacheKey<TKey> internalKey = new TimedCacheKey<TKey>(key, slidingExpiration);
                timedStorage.Add(internalKey);
                timedStorageIndex.Add(key, internalKey);
                return true;
            }
            finally
            {
                Monitor.Exit(syncRoot);
            }
        }

        public void CopyTo(Array array, int startIndex)
        {
            // Error checking
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException("startIndex", "startIndex must be >= 0.");
            }

            if (array.Rank > 1)
            {
                throw new ArgumentException("array must be of Rank 1 (one-dimensional)", "array");
            }
            if (startIndex >= array.Length)
            {
                throw new ArgumentException("startIndex must be less than the length of the array.", "startIndex");
            }
            if (Count > array.Length - startIndex)
            {
                throw new ArgumentException(
                    "There is not enough space from startIndex to the end of the array to accomodate all items in the cache.");
            }

            // Copy the data to the array (in a thread-safe manner)
            if (!Monitor.TryEnter(syncRoot, MAX_LOCK_WAIT))
                throw new ApplicationException("Lock could not be acquired after " + MAX_LOCK_WAIT + "ms");
            try
            {
                foreach (object o in timedStorage)
                {
                    array.SetValue(o, startIndex);
                    startIndex++;
                }
            }
            finally
            {
                Monitor.Exit(syncRoot);
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        ///     Purges expired objects from the cache. Called automatically by the purge timer.
        /// </summary>
        private void PurgeCache(object sender, ElapsedEventArgs e)
        {
            // Only let one thread purge at once - a buildup could cause a crash
            // This could cause the purge to be delayed while there are lots of read/write ops 
            // happening on the cache
            if (!Monitor.TryEnter(isPurging))
                return;

            DateTime signalTime = DateTime.UtcNow;

            try
            {
                // If we fail to acquire a lock on the synchronization root after MAX_LOCK_WAIT, skip this purge cycle
                if (!Monitor.TryEnter(syncRoot, MAX_LOCK_WAIT))
                    return;
                try
                {
                    Lazy<List<object>> expiredItems = new Lazy<List<object>>();
#if (!ISWIN)
                    foreach (TimedCacheKey<TKey> timedKey in timedStorage)
                    {
                        if (timedKey.ExpirationDate < signalTime)
                        {
                            // Mark the object for purge
                            expiredItems.Value.Add(timedKey.Key);
                        }
                    }
#else
                    foreach (
                        TimedCacheKey<TKey> timedKey in
                            timedStorage.Where(timedKey => timedKey.ExpirationDate < signalTime))
                    {
                        // Mark the object for purge
                        expiredItems.Value.Add(timedKey.Key);
                    }
#endif

                    if (expiredItems.IsValueCreated)
                    {
                        foreach (
                            TimedCacheKey<TKey> timedKey in
                                from TKey key in expiredItems.Value select timedStorageIndex[key])
                        {
                            timedStorageIndex.Remove(timedKey.Key);
                            timedStorage.Remove(timedKey);
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(syncRoot);
                }
            }
            finally
            {
                Monitor.Exit(isPurging);
            }
        }

        #endregion
    }
}