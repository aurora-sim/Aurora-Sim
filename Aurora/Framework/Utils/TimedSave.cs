using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using System.Timers;

namespace Aurora.Framework
{
    public class TimedSaving<T>
    {
        public delegate void TimeElapsed(UUID agentID, T data);

        private readonly Dictionary<UUID, T> _saveQueueData = new Dictionary<UUID, T>();
        private readonly Dictionary<UUID, long> _queue = new Dictionary<UUID, long>();

        private readonly Timer _updateTimer = new Timer();
        private const int _checkTime = 500; // milliseconds to wait between checks for updates
        private int _sendtime = 2;
        private TimeElapsed _arg;

        public void Start(int secondsToWait, TimeElapsed args)
        {
            _arg = args;
            _sendtime = secondsToWait;
            _updateTimer.Enabled = false;
            _updateTimer.AutoReset = true;
            _updateTimer.Interval = _checkTime; // 500 milliseconds wait to start async ops
            _updateTimer.Elapsed += timer_elapsed;
        }

        public void Add(UUID agentid)
        {
            long timestamp = DateTime.Now.Ticks + Convert.ToInt64(_sendtime * 1000 * 10000);
            lock (_queue)
            {
                _queue[agentid] = timestamp;
                _updateTimer.Start();
            }
        }

        public void Add(UUID agentid, T data)
        {
            long timestamp = DateTime.Now.Ticks + Convert.ToInt64(_sendtime * 1000 * 10000);
            lock (_queue)
            {
                _queue[agentid] = timestamp;
                _saveQueueData[agentid] = data;
                _updateTimer.Start();
            }
        }

        private void timer_elapsed(object sender, EventArgs ea)
        {
            long now = DateTime.Now.Ticks;

            Dictionary<UUID, long> sends;
            lock (_queue)
                sends = new Dictionary<UUID, long>(_queue);

            foreach (KeyValuePair<UUID, long> kvp in sends)
            {
                if (kvp.Value < now)
                {
                    T data = default(T);
                    lock (_saveQueueData)
                        if (_saveQueueData.TryGetValue(kvp.Key, out data))
                            _saveQueueData.Remove(kvp.Key);
                    Util.FireAndForget(delegate { _arg(kvp.Key, data); });
                    lock (_queue)
                        _queue.Remove(kvp.Key);
                }
            }
        }
    }

    public class ListCombiningTimedSaving<T>
    {
        public delegate void TimeElapsed(UUID agentID, List<T> data);

        private readonly Dictionary<UUID, List<T>> _saveQueueData = new Dictionary<UUID, List<T>>();
        private readonly Dictionary<UUID, long> _queue = new Dictionary<UUID, long>();

        private readonly Timer _updateTimer = new Timer();
        private const int _checkTime = 500; // milliseconds to wait between checks for updates
        private int _sendtime = 3;
        private TimeElapsed _arg;

        public void Start(int secondsToWait, TimeElapsed args)
        {
            _arg = args;
            _sendtime = secondsToWait;
            _updateTimer.Enabled = false;
            _updateTimer.AutoReset = true;
            _updateTimer.Interval = _checkTime; // 500 milliseconds wait to start async ops
            _updateTimer.Elapsed += timer_elapsed;
        }

        public void Add(UUID agentid)
        {
            long timestamp = DateTime.Now.Ticks + Convert.ToInt64(_sendtime * 1000 * 10000);
            lock (_queue)
            {
                _queue[agentid] = timestamp;
                _updateTimer.Start();
            }
        }

        public void Add(UUID agentid, List<T> data)
        {
            long timestamp = DateTime.Now.Ticks + Convert.ToInt64(_sendtime * 1000 * 10000);
            lock (_queue)
            {
                _queue[agentid] = timestamp;
                if (!_saveQueueData.ContainsKey(agentid))
                    _saveQueueData.Add(agentid, new List<T>());
                _saveQueueData[agentid].AddRange(data);
                _updateTimer.Start();
            }
        }

        private void timer_elapsed(object sender, EventArgs ea)
        {
            long now = DateTime.Now.Ticks;

            Dictionary<UUID, long> sends;
            lock (_queue)
                sends = new Dictionary<UUID, long>(_queue);

            foreach (KeyValuePair<UUID, long> kvp in sends)
            {
                if (kvp.Value < now)
                {
                    List<T> data = new List<T>();
                    lock (_saveQueueData)
                        if (_saveQueueData.TryGetValue(kvp.Key, out data))
                            _saveQueueData.Remove(kvp.Key);
                    Util.FireAndForget(delegate { _arg(kvp.Key, data); });
                    lock (_queue)
                        _queue.Remove(kvp.Key);
                }
            }
        }
    }
}
