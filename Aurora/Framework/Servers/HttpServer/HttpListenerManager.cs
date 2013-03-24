using Aurora.Framework.ConsoleFramework;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;

namespace Aurora.Framework.Servers.HttpServer
{
    public class HttpListenerManager : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly Thread _listenerThread;
        private readonly Thread[] _workers;
        private readonly ManualResetEvent _stop, _ready;
        private ConcurrentQueue<HttpListenerContext> _queue;
        public event Action<HttpListenerContext> ProcessRequest;
        private bool _isSecure = false;

        public HttpListenerManager(int maxThreads, bool isSecure)
        {
            _workers = new Thread[maxThreads];
            _queue = new ConcurrentQueue<HttpListenerContext>();
            _stop = new ManualResetEvent(false);
            _ready = new ManualResetEvent(false);
            _listener = new HttpListener();
            _listenerThread = new Thread(HandleRequests);
            _isSecure = isSecure;
        }

        public void Start(uint port)
        {
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
            _stop.Set();
            _listenerThread.Join();
            foreach (Thread worker in _workers)
                worker.Join();
            _listener.Stop();
        }

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
                _queue.Enqueue(_listener.EndGetContext(ar));
                _ready.Set();
            }
            catch
            {
                return;
            }
        }

        private void Worker()
        {
            WaitHandle[] wait = new[] {_ready, _stop};
            while (0 == WaitHandle.WaitAny(wait))
            {
                HttpListenerContext context;
                if (!_queue.TryDequeue(out context))
                {
                    _ready.Reset();
                    continue;
                }

                try
                {
                    ProcessRequest(context);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                }
            }
        }
    }
}