using System;
using System.Threading;
using System.Collections.Generic;
using OpenMetaverse;

namespace Aurora.Framework
{
    #region TimedCacheKey Class

    class TimedCacheKey<TKey> : IComparable<TKey>
    {
        private DateTime expirationDate;
        private bool slidingExpiration;
        private TimeSpan slidingExpirationWindowSize;
        private TKey key;

        public DateTime ExpirationDate { get { return expirationDate; } }
        public TKey Key { get { return key; } }
        public bool SlidingExpiration { get { return slidingExpiration; } }
        public TimeSpan SlidingExpirationWindowSize { get { return slidingExpirationWindowSize; } }

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

        public void Accessed()
        {
            if (slidingExpiration)
                expirationDate = DateTime.Now.Add(slidingExpirationWindowSize);
        }

        public int CompareTo(TKey other)
        {
            return key.GetHashCode().CompareTo(other.GetHashCode());
        }
    }

    #endregion

    public sealed class ExpiringList<TKey>
    {
        const double CACHE_PURGE_HZ = 1.0;
        const int MAX_LOCK_WAIT = 5000; // milliseconds

        #region Private fields

        /// <summary>For thread safety</summary>
        object syncRoot = new object();
        /// <summary>For thread safety</summary>
        object isPurging = new object();

        List<TimedCacheKey<TKey>> timedStorage = new List<TimedCacheKey<TKey>>();
        Dictionary<TKey, TimedCacheKey<TKey>> timedStorageIndex = new Dictionary<TKey, TimedCacheKey<TKey>>();
        private System.Timers.Timer timer = new System.Timers.Timer(TimeSpan.FromSeconds(CACHE_PURGE_HZ).TotalMilliseconds);

        #endregion

        #region Constructor

        public ExpiringList()
        {
            timer.Elapsed += PurgeCache;
            timer.Start();
        }

        #endregion

        #region Public methods

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
                    TimedCacheKey<TKey> internalKey = new TimedCacheKey<TKey>(key, DateTime.UtcNow + TimeSpan.FromSeconds(expirationSeconds));
                    timedStorage.Add(internalKey);
                    timedStorageIndex.Add(key, internalKey);
                    return true;
                }
            }
            finally { Monitor.Exit(syncRoot); }
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
            finally { Monitor.Exit(syncRoot); }
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
            finally { Monitor.Exit(syncRoot); }
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
            finally { Monitor.Exit(syncRoot); }
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
            finally { Monitor.Exit(syncRoot); }
        }

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
                finally { Monitor.Exit(syncRoot); }
            }
        }

        public TKey this[int i, double time]
        {
            set
            {
                AddOrUpdate(value, time);
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
            finally { Monitor.Exit(syncRoot); }
        }

        public int Count
        {
            get
            {
                return timedStorage.Count;
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
            finally { Monitor.Exit(syncRoot); }
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
            finally { Monitor.Exit(syncRoot); }
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

                TimedCacheKey<TKey> internalKey = new TimedCacheKey<TKey>(key, DateTime.UtcNow + TimeSpan.FromSeconds(expirationSeconds));
                timedStorage.Add(internalKey);
                timedStorageIndex.Add(key, internalKey);
                return true;
            }
            finally { Monitor.Exit(syncRoot); }
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
            finally { Monitor.Exit(syncRoot); }
        }

        public void CopyTo(Array array, int startIndex)
        {
            // Error checking
            if (array == null) { throw new ArgumentNullException("array"); }

            if (startIndex < 0) { throw new ArgumentOutOfRangeException("startIndex", "startIndex must be >= 0."); }

            if (array.Rank > 1) { throw new ArgumentException("array must be of Rank 1 (one-dimensional)", "array"); }
            if (startIndex >= array.Length) { throw new ArgumentException("startIndex must be less than the length of the array.", "startIndex"); }
            if (Count > array.Length - startIndex) { throw new ArgumentException("There is not enough space from startIndex to the end of the array to accomodate all items in the cache."); }

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
            finally { Monitor.Exit(syncRoot); }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Purges expired objects from the cache. Called automatically by the purge timer.
        /// </summary>
        private void PurgeCache(object sender, System.Timers.ElapsedEventArgs e)
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

                    foreach (TimedCacheKey<TKey> timedKey in timedStorage)
                    {
                        if (timedKey.ExpirationDate < signalTime)
                        {
                            // Mark the object for purge
                            expiredItems.Value.Add(timedKey.Key);
                        }
                    }

                    if (expiredItems.IsValueCreated)
                    {
                        foreach (TKey key in expiredItems.Value)
                        {
                            TimedCacheKey<TKey> timedKey = timedStorageIndex[key];
                            timedStorageIndex.Remove(timedKey.Key);
                            timedStorage.Remove(timedKey);
                        }
                    }
                }
                finally { Monitor.Exit(syncRoot); }
            }
            finally { Monitor.Exit(isPurging); }
        }

        #endregion
    }
}
