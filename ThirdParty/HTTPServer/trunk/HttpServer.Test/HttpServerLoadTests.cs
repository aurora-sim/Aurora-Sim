using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using HttpServer.HttpModules;
using HttpServer.Sessions;
using Xunit;

namespace HttpServer.Test
{
    public class HttpServerLoadTests
    {
        private static readonly ManualResetEvent _threadsGoEvent = new ManualResetEvent(false);
        private static readonly ManualResetEvent _threadsDoneEvent = new ManualResetEvent(false);
        private const int ThreadCount = 300;
        private static int _currentThreadCount;
        private static List<string> _failedThreads = new List<string>();


        [Fact]
        public void Test()
        {
			HttpServer server = new HttpServer();
            server.Add(new SimpleModule());
            server.Start(IPAddress.Any, 8899);
            server.BackLog = 50;

            Thread[] threads = new Thread[ThreadCount];
            for (int i = 0; i < ThreadCount; ++i)
            {
                threads[i] = new Thread(OnSendRequest);
                threads[i].Start(i+1);
            }

            if (!_threadsDoneEvent.WaitOne(60000, true))
                Console.WriteLine("Failed to get all responses.");

            foreach (string s in _failedThreads)
                Console.WriteLine("* Failed thread: " + s);
            if (_failedThreads.Count  > 0)
                Console.WriteLine("* Total: " + _failedThreads.Count);

            Console.ReadLine();
        }

        private static void OnSendRequest(object state)
        {
            string id = state.ToString().PadLeft(3, '0');
            WebClient client = new WebClient();

            Interlocked.Increment(ref _currentThreadCount);
            if (_currentThreadCount == ThreadCount)
                _threadsGoEvent.Set();
            
            // thread should wait until all threads are ready, to try the server.
            if (!_threadsGoEvent.WaitOne(60000, true))
                Assert.False(true, "Start event was not triggered.");

            try
            {
                client.UploadString(new Uri("http://localhost:8899/?id=" + id), "GET");
            }
            catch(WebException)
            {
                _failedThreads.Add(id);
            }
            Console.WriteLine(id + " done, left: " + _currentThreadCount);

            // last thread should signal main test
            Interlocked.Decrement(ref _currentThreadCount);
            if (_currentThreadCount == 0)
                _threadsDoneEvent.Set();
        }

        private class SimpleModule : HttpModule
        {
            public override bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
            {
                Console.WriteLine(request.QueryString["id"].Value + " got request");
                response.Status = HttpStatusCode.OK;

                byte[] buffer = Encoding.ASCII.GetBytes(request.QueryString["id"].Value);
                response.Body.Write(buffer, 0, buffer.Length);

                return true;
            }
        }


    }
}
