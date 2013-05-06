using Aurora.Framework.ConsoleFramework;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;

namespace Aurora.Framework.Servers.HttpServer
{
    public sealed class HttpListenerManager : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly Thread _listenerThread;
        private readonly Thread[] _workers;
        private ConcurrentQueue<HttpListenerContext> _queue;
        public event Action<HttpListenerContext> ProcessRequest;
        private ManualResetEvent _newQueueItem = new ManualResetEvent(false), _listenForNextRequest = new ManualResetEvent(false);
        private bool _isSecure = false;
        private bool _isRunning = false;
        private int _lockedQueue = 0;

        public HttpListenerManager(uint maxThreads, bool isSecure)
        {
            _workers = new Thread[maxThreads];
            _queue = new ConcurrentQueue<HttpListenerContext>();
            _listener = new HttpListener();
#if LINUX
            _listener.IgnoreWriteExceptions = true;
#endif
            _listenerThread = new Thread(HandleRequests);
            _isSecure = isSecure;
        }

        public void Start(uint port)
        {
            _isRunning = true;
            _listener.Prefixes.Add(String.Format(@"http{0}://+:{1}/", _isSecure ? "s" : "", port));
            _listener.Start();
            _listenerThread.Start();

            for (int i = 0; i < _workers.Length; i++)
            {
                _workers[i] = new Thread(Worker);
                _workers[i].Start();
            }
        }

        public void Dispose()
        {
            Stop();
        }

        public void Stop()
        {
            if (!_isRunning)
                return;
            _isRunning = false;
#if true //LINUX
            _listenerThread.Abort();
            foreach (Thread worker in _workers)
                worker.Abort();
#else
            _listenerThread.Join();
            foreach (Thread worker in _workers)
                worker.Join();
#endif
            _listener.Stop();
            _listener.Close();
        }

#if true //LINUX

        private void HandleRequests()
        {
            while (_listener.IsListening)
            {
                _listener.BeginGetContext(ListenerCallback, null);
                _listenForNextRequest.WaitOne();
                _listenForNextRequest.Reset();
            }
        }

        private void ListenerCallback(IAsyncResult result)
        {
            HttpListenerContext context = null;

            try
            {
                context = _listener.EndGetContext(result);
            }
            catch (Exception ex)
            {
                MainConsole.Instance.ErrorFormat("[HttpListenerManager]: Exception occured: {0}", ex.ToString());
                return;
            }
            finally
            {
                _listenForNextRequest.Set();
            }
            if (context == null)
                return;
            _queue.Enqueue(context);
            _newQueueItem.Set();
        }

#else

        private void HandleRequests()
        {
            while (_listener.IsListening)
            {
                var context = _listener.BeginGetContext(ContextReady, null);

                if (0 == WaitHandle.WaitAny(new[] {_stop, context.AsyncWaitHandle}))
                    return;
            }
        }

        private void ContextReady(IAsyncResult ar)
        {
            try
            {
                if (!_listener.IsListening) return;
                _queue.Enqueue(_listener.EndGetContext(ar));
                _newQueueItem.Set();
            }
            catch
            {
                return;
            }
        }

#endif

        private void Worker()
        {
            while ((_queue.Count > 0 || _newQueueItem.WaitOne()) && _listener.IsListening)
            {
                _newQueueItem.Reset();
                HttpListenerContext context = null;
                if (Interlocked.CompareExchange(ref _lockedQueue, 1, 0) == 0)
                {
                    _queue.TryDequeue(out context);
                    //All done
                    Interlocked.Exchange(ref _lockedQueue, 0);
                }
                try
                {
                    if(context != null)
                        ProcessRequest(context);
                }
                catch (Exception e)
                {
                    MainConsole.Instance.ErrorFormat("[HttpListenerManager]: Exception occured: {0}", e.ToString());
                }
            }
        }
    }
}
