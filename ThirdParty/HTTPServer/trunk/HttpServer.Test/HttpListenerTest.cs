using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Xunit;

namespace HttpServer.Test
{
    public class HttpListenerTest
    {
        private readonly ManualResetEvent _testEvent;
        private readonly List<Thread> _threads = new List<Thread>();
        private readonly ManualResetEvent _waitOnAllThreads;
        private int _currentThreadCount;
        private int _threadCount;

        public HttpListenerTest()
        {
            _testEvent = new ManualResetEvent(false);
            _waitOnAllThreads = new ManualResetEvent(false);
            _currentThreadCount = 0;
        }

        [Fact]
        public void TestMultiThreaded()
        {
            HttpListener listener = HttpListener.Create(IPAddress.Any, 8321);
            listener.RequestReceived += OnIncomingRequest;
            listener.Start(20);
            LaunchThreads(50, OnTestMultiThreaded);

            // check if all was invoked and got result
            WaitOnThreadsToComplete();
            listener.Stop();
            Assert.Equal(_threadCount, _currentThreadCount);
        }

        /// <summary>
        /// Launch a bunch on client threads. then start/stop the listener.
        /// 
        /// Purpose of this test is to make sure that the listener do not die if
        /// we start/stop it during incoming connections.
        /// </summary>
        [Fact]
        public void TestStartStop()
        {
            HttpListener listener = HttpListener.Create(IPAddress.Any, 8321);
            listener.LogWriter = NullLogWriter.Instance;
            listener.RequestReceived += OnIncomingRequest;
            listener.Start(5);

            LaunchThreads(10, OnTestStartStop);
            Thread.Sleep(50);
            for (int i = 0; i < 50; ++i)
            {
                listener.Stop();
                listener.Start(1);
            }
            listener.Stop();

            foreach (WebClient client in _webClients)
                client.Dispose();

            // the aborting hangs, and I do not know why :/
            //foreach (Thread thread in _threads)
            //    thread.Abort();
        }

        /// <summary>
        /// Purpose is to see if we can still accept connections after we have started and stopped the listener.
        /// </summary>
        [Fact]
        public void TestSequential()
        {
            WebClient _client = new WebClient();

            for (int i = 0; i < 100; ++i)
            {
                _testEvent.Reset();
                HttpListener listener = HttpListener.Create(IPAddress.Any, 4324);
                listener.RequestReceived += OnIncomingRequest;
                listener.Start(5);
                _client.DownloadString("http://localhost:4324/welcome/to/tomorrow/");
                listener.Stop();
                Console.WriteLine("Round: " + i);
                Assert.True(_testEvent.WaitOne(5000, false));
            }
        }

        private readonly List<WebClient> _webClients = new List<WebClient>();
        private void OnTestStartStop(object obj)
        {
            WaitOnAllThreads();
            try
            {
                WebClient client = new WebClient();
                _webClients.Add(client);
                Assert.Equal("Hello world!", client.DownloadString("http://localhost:8321/welcome/to/tomorrow/"));
                Assert.Equal("Hello world!", client.DownloadString("http://localhost:8321/welcome/to/tomorrow/"));
            }
            catch(WebException)
            {                
            }
            catch (ThreadAbortException)
            {                
            }
        }

        private void WaitOnThreadsToComplete()
        {
            foreach (Thread thread in _threads)
                thread.Join(15000);

            _threads.Clear();
        }

        /// <summary>
        /// used to launch all worker threads
        /// </summary>
        /// <param name="count">how many threads to use</param>
        private void LaunchThreads(int count, ParameterizedThreadStart threadStart)
        {
            _threadCount = count;

            for (int i = 0; i < _threadCount; ++i)
            {
                Thread thread = new Thread(threadStart);
                thread.Name = i.ToString();
                thread.Start(i);
                _threads.Add(thread);
            }

            // Wait for all threads.
            _waitOnAllThreads.WaitOne(5000, false);
            _waitOnAllThreads.Reset();
            Assert.Equal(_threadCount, _currentThreadCount);

            // start to execute threads
            _currentThreadCount = 0;
            _testEvent.Set();
        }

        /// <summary>
        /// Used in worker threads to not start working until all
        /// worker threads have been launched.
        /// </summary>
        private void WaitOnAllThreads()
        {
            ++_currentThreadCount;
            if (_currentThreadCount == _threadCount)
                _waitOnAllThreads.Set();
            _testEvent.WaitOne();
        }

        /// <summary>
        /// Workerthread 
        /// </summary>
        /// <param name="index"></param>
        public void OnTestMultiThreaded(object index)
        {
            int i = (int) index;
            WaitOnAllThreads();
            WebClient _client = new WebClient();
            try
            {
                Assert.Equal("Hello world!", _client.DownloadString("http://localhost:8321/" + i));
            }
            catch (WebException)
            {
                Thread.Sleep(150);
                Assert.Equal("Hello world!", _client.DownloadString("http://localhost:8321/" + i));
            }
            if (_currentThreadCount == _threadCount)
                _waitOnAllThreads.Set();
        }

        private void OnIncomingRequest(object source, RequestEventArgs args)
        {
            IHttpClientContext client = (IHttpClientContext)source;
            IHttpRequest request = args.Request;
            client.Respond("Hello world!");
            ++_currentThreadCount;
            _testEvent.Set();
        }

    }
}