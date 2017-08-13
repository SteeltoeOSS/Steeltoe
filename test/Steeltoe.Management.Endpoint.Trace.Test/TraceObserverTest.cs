using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.Trace.Test
{
    public class TraceObserverTest : BaseTest
    {
        [Fact]
        public void Constructor_ThrowsOnNulls()
        {
            // Arrange
            DiagnosticListener listener = null;
            DiagnosticListener listener2 = new DiagnosticListener("test");
            ITraceOptions options = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new TraceObserver(listener, options));
            Assert.Contains(nameof(listener), ex.Message);
            var ex2 = Assert.Throws<ArgumentNullException>(() => new TraceObserver(listener2, options));
            Assert.Contains(nameof(options), ex2.Message);


            listener2.Dispose();
        }
        [Fact]
        public void GetSessionId_NoSession_ReturnsExpected()
        {
            DiagnosticListener listener = new DiagnosticListener("test");
            TraceOptions option = new TraceOptions();

            TraceObserver obs = new TraceObserver(listener, option);
            HttpContext context = CreateRequest();
            var result = obs.GetSessionId(context);
            Assert.Null(result);
            listener.Dispose();
        }

        [Fact]
        public void GetSessionId_WithSession_ReturnsExpected()
        {
            DiagnosticListener listener = new DiagnosticListener("test");
            TraceOptions option = new TraceOptions();

            TraceObserver obs = new TraceObserver(listener, option);
            HttpContext context = CreateRequest();

            var session = new TestSession();
            ISessionFeature sessFeature = new SessionFeature();
            sessFeature.Session = session;
            context.Features.Set<ISessionFeature>(sessFeature);

            var result = obs.GetSessionId(context);
            Assert.Equal("TestSessionId", result);
            listener.Dispose();
        }

        [Fact]
        public void GetUserPrincipal_NotAuthenticated_ReturnsExpected()
        {
            DiagnosticListener listener = new DiagnosticListener("test");
            TraceOptions option = new TraceOptions();

            TraceObserver obs = new TraceObserver(listener, option);
            HttpContext context = CreateRequest();
            var result = obs.GetUserPrincipal(context);
            Assert.Null(result);
            listener.Dispose();
        }

        [Fact]
        public void GetUserPrincipal_Authenticated_ReturnsExpected()
        {
            DiagnosticListener listener = new DiagnosticListener("test");
            TraceOptions option = new TraceOptions();

            TraceObserver obs = new TraceObserver(listener, option);
            HttpContext context = CreateRequest();

            context.User = new ClaimsPrincipal(new MyIdentity());
            var result = obs.GetUserPrincipal(context);
            Assert.Equal("MyTestName", result);
            listener.Dispose();
        }
        [Fact]
        public void GetRemoteAddress_NoConnection_ReturnsExpected()
        {
            DiagnosticListener listener = new DiagnosticListener("test");
            TraceOptions option = new TraceOptions();

            TraceObserver obs = new TraceObserver(listener, option);
            HttpContext context = CreateRequest();
            var result = obs.GetRemoteAddress(context);
            Assert.Null(result);
            listener.Dispose();

        }

        [Fact]
        public void GetPathInfo_ReturnsExpected()
        {
            DiagnosticListener listener = new DiagnosticListener("test");
            TraceOptions option = new TraceOptions();

            TraceObserver obs = new TraceObserver(listener, option);
            HttpContext context = CreateRequest();
            
            var result = obs.GetPathInfo(context.Request);
            Assert.Equal("/myPath", result);
            listener.Dispose();

        }
        [Fact]
        public void GetRequestUri_ReturnsExpected()
        {
            DiagnosticListener listener = new DiagnosticListener("test");
            TraceOptions option = new TraceOptions();

            TraceObserver obs = new TraceObserver(listener, option);
            HttpContext context = CreateRequest();
            var result = obs.GetRequestUri(context.Request);
            Assert.Equal("http://localhost:1111/myPath", result);
            listener.Dispose();
        }

        [Fact]
        public void GetRequestParameters_ReturnsExpected()
        {
            DiagnosticListener listener = new DiagnosticListener("test");
            TraceOptions option = new TraceOptions();

            TraceObserver obs = new TraceObserver(listener, option);
            HttpContext context = CreateRequest();
            var result = obs.GetRequestParameters(context.Request);
            Assert.NotNull(result);
            Assert.True(result.ContainsKey("foo"));
            Assert.True(result.ContainsKey("bar"));
            var fooVal = result["foo"];
            Assert.Equal(1, fooVal.Length);
            Assert.Equal("bar", fooVal[0]);
            var barVal = result["bar"];
            Assert.Equal(1, barVal.Length);
            Assert.Equal("foo", barVal[0]);
            listener.Dispose();
        }

        [Fact]
        public void GetTimeTaken_ReturnsExpected()
        {
            DiagnosticListener listener = new DiagnosticListener("test");
            TraceOptions option = new TraceOptions();

            TraceObserver obs = new TraceObserver(listener, option);
            HttpContext context = CreateRequest();
            var result = obs.GetTimeTaken(10000000);
            var expected = (10000000 / obs.ticksPerMilli).ToString();
            Assert.Equal(expected, result);

            listener.Dispose();
        }
        [Fact]
        public void GetHeaders_ReturnsExpected()
        {
            DiagnosticListener listener = new DiagnosticListener("test");
            TraceOptions option = new TraceOptions();

            TraceObserver obs = new TraceObserver(listener, option);
            HttpContext context = CreateRequest();

            var result = obs.GetHeaders(100, context.Request.Headers);
            Assert.NotNull(result);
            Assert.True(result.ContainsKey("header1"));
            Assert.True(result.ContainsKey("header2"));
            Assert.True(result.ContainsKey("status"));
            var header1Val = result["header1"] as string;
            Assert.Equal("header1Value", header1Val);
            var header2Val = result["header2"] as string;
            Assert.Equal("header2Value", header2Val);
            var statusVal = result["status"] as string;
            Assert.Equal("100", statusVal);
            listener.Dispose();
        }

        [Fact]
        public void GetProperties_NoProperties_ReturnsExpected()
        {
            DiagnosticListener listener = new DiagnosticListener("test");
            TraceOptions option = new TraceOptions();

            TraceObserver obs = new TraceObserver(listener, option);
            long timeStamp = -1;
            HttpContext context = null;

            obs.GetProperties(new { foo = "bar" }, out context, out timeStamp);
            Assert.Equal(0, timeStamp);
            Assert.Null(context);
            listener.Dispose();
        }

        [Fact]
        public void GetProperties_WithProperties_ReturnsExpected()
        {
            DiagnosticListener listener = new DiagnosticListener("test");
            TraceOptions option = new TraceOptions();

            TraceObserver obs = new TraceObserver(listener, option);
            long timeStamp = -1;
            HttpContext context = null;
            long expectedTime = Stopwatch.GetTimestamp();
            var expectedContext = CreateRequest();

            obs.GetProperties(new { httpContext = expectedContext, timestamp = expectedTime }, out context, out timeStamp);
            Assert.Equal(expectedTime, timeStamp);
            Assert.True(object.ReferenceEquals(expectedContext, context));

            listener.Dispose();
        }

        [Fact]
        public void MakeTrace_ReturnsExpected()
        {
            DiagnosticListener listener = new DiagnosticListener("test");
            TraceOptions option = new TraceOptions();

            TraceObserver obs = new TraceObserver(listener, option);
            HttpContext context = CreateRequest();
            Trace result = obs.MakeTrace(context, 10000000, 20000000);
            Assert.NotNull(result);
            Assert.NotNull(result.Info);
            Assert.NotEqual(0, result.TimeStamp);
            Assert.True(result.Info.ContainsKey("method"));
            Assert.True(result.Info.ContainsKey("path"));
            Assert.True(result.Info.ContainsKey("headers"));
            Assert.True(result.Info.ContainsKey("timeTaken"));
            Assert.Equal("GET", result.Info["method"]);
            Assert.Equal("/myPath", result.Info["path"]);
            var headers = result.Info["headers"] as Dictionary<string, object>;
            Assert.NotNull(headers);
            Assert.True(headers.ContainsKey("request"));
            Assert.True(headers.ContainsKey("response"));
            var timeTaken = result.Info["timeTaken"] as string;
            Assert.NotNull(timeTaken);
            var expected = ((20000000 - 10000000) / obs.ticksPerMilli).ToString();
            Assert.Equal(expected, timeTaken);
            listener.Dispose();
        }

        [Fact]
        public void OnNext_IgnoresUnRecognizedEvents()
        {
            DiagnosticListener listener = new DiagnosticListener("test");
            TraceOptions option = new TraceOptions();

            TraceObserver obs = new TraceObserver(listener, option);

            KeyValuePair<string, object> ignore = new KeyValuePair<string, object>("foobar", null);
            obs.OnNext(ignore);
            Assert.Equal(0, obs._pending.Count);
            Assert.Equal(0, obs._queue.Count);
            listener.Dispose();
        }

        [Fact]
        public void OnNext_AddsToPending()
        {
            DiagnosticListener listener = new DiagnosticListener("test");
            TraceOptions option = new TraceOptions();

            TraceObserver obs = new TraceObserver(listener, option);
            long expectedTime = Stopwatch.GetTimestamp();
            var expectedContext = CreateRequest();

            KeyValuePair<string, object> begin = new KeyValuePair<string, object>(TraceObserver.BEGIN_REQUEST, 
                new { httpContext = expectedContext, timestamp = expectedTime });

            obs.OnNext(begin);

            Assert.Equal(1, obs._pending.Count);
            Assert.True(obs._pending.ContainsKey(expectedContext.TraceIdentifier));
            var pending = obs._pending[expectedContext.TraceIdentifier];
            Assert.Equal(expectedTime, pending.StartTime);

            Assert.Equal(0, obs._queue.Count);
            listener.Dispose();
        }

        [Fact]
        public void OnNext_RemovesPending_AddsToQueue()
        {
            DiagnosticListener listener = new DiagnosticListener("test");
            TraceOptions option = new TraceOptions();

            TraceObserver obs = new TraceObserver(listener, option);
            long start = Stopwatch.GetTimestamp();
            var context = CreateRequest();

            KeyValuePair<string, object> begin = new KeyValuePair<string, object>(TraceObserver.BEGIN_REQUEST,
                new { httpContext = context, timestamp = start });
            obs.OnNext(begin);

            KeyValuePair<string, object> end = new KeyValuePair<string, object>(TraceObserver.END_REQUEST,
                new { httpContext = context, timestamp = start + 100000});
            obs.OnNext(end);

            Assert.Equal(0, obs._pending.Count);
            Assert.Equal(1, obs._queue.Count);

            Trace result = null;
            Assert.True(obs._queue.TryPeek(out result));
            Assert.NotNull(result.Info);
            Assert.NotEqual(0, result.TimeStamp);
            Assert.True(result.Info.ContainsKey("method"));
            Assert.True(result.Info.ContainsKey("path"));
            Assert.True(result.Info.ContainsKey("headers"));
            Assert.True(result.Info.ContainsKey("timeTaken"));
            Assert.Equal("GET", result.Info["method"]);
            Assert.Equal("/myPath", result.Info["path"]);
            var headers = result.Info["headers"] as Dictionary<string, object>;
            Assert.NotNull(headers);
            Assert.True(headers.ContainsKey("request"));
            Assert.True(headers.ContainsKey("response"));
            var timeTaken = result.Info["timeTaken"] as string;
            Assert.NotNull(timeTaken);
            var expected = ((100000) / obs.ticksPerMilli).ToString();
            Assert.Equal(expected, timeTaken);

            listener.Dispose();
        }
        [Fact]
        public void OnNext_HonorsCapacity()
        {
            DiagnosticListener listener = new DiagnosticListener("test");
            TraceOptions option = new TraceOptions();

            TraceObserver obs = new TraceObserver(listener, option);
            for (int i = 0; i < 200; i++)
            {
                long start = Stopwatch.GetTimestamp();
                var context = CreateRequest();

                KeyValuePair<string, object> begin = new KeyValuePair<string, object>(TraceObserver.BEGIN_REQUEST,
                    new { httpContext = context, timestamp = start });
                obs.OnNext(begin);

                KeyValuePair<string, object> end = new KeyValuePair<string, object>(TraceObserver.END_REQUEST,
                    new { httpContext = context, timestamp = start + 100000 });
                obs.OnNext(end);

            }
            Assert.Equal(0, obs._pending.Count);
            Assert.Equal(option.Capacity, obs._queue.Count);

            listener.Dispose();
        }
        [Fact]
        public void GetTraces_ReturnsTraces()
        {
            DiagnosticListener listener = new DiagnosticListener("test");
            TraceOptions option = new TraceOptions();

            TraceObserver obs = new TraceObserver(listener, option);
            for (int i = 0; i < 200; i++)
            {
                long start = Stopwatch.GetTimestamp();
                var context = CreateRequest();

                KeyValuePair<string, object> begin = new KeyValuePair<string, object>(TraceObserver.BEGIN_REQUEST,
                    new { httpContext = context, timestamp = start });
                obs.OnNext(begin);

                KeyValuePair<string, object> end = new KeyValuePair<string, object>(TraceObserver.END_REQUEST,
                    new { httpContext = context, timestamp = start + 100000 });
                obs.OnNext(end);

            }
            Assert.Equal(0, obs._pending.Count);
            Assert.Equal(option.Capacity, obs._queue.Count);
            List<Trace> traces = obs.GetTraces();
            Assert.Equal(option.Capacity, traces.Count);
            Assert.Equal(option.Capacity, obs._queue.Count);

            listener.Dispose();
        }
        private HttpContext CreateRequest()
        {
            HttpContext context = new DefaultHttpContext();
            context.TraceIdentifier = Guid.NewGuid().ToString();
            context.Response.Body = new MemoryStream();
            context.Request.Method = "GET";
            context.Request.Path = new PathString("/myPath");
            context.Request.Scheme = "http";
            context.Request.Host = new HostString("localhost:1111");
            context.Request.QueryString = new QueryString("?foo=bar&bar=foo");
            context.Request.Headers.Add("Header1", new StringValues("header1Value"));
            context.Request.Headers.Add("Header2", new StringValues("header2Value"));
            return context;
        }

    }

    class MyIdentity : IIdentity
    {
        public string Name { get; } = "MyTestName";

        public string AuthenticationType { get; } = "MyTestAuthType";

        public bool IsAuthenticated { get; } = true;
    }
    class SessionFeature : ISessionFeature
    {
        public ISession Session { get; set; }
    }
    class TestSession : ISession
    {
        private Dictionary<string, byte[]> _store
                = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        public bool IsAvailable { get; } = true;

        public string Id { get; set; } = "TestSessionId";

        public IEnumerable<string> Keys { get { return _store.Keys; } }

        public void Clear()
        {
            _store.Clear();
        }

        public Task CommitAsync()
        {
            return Task.FromResult(0);   
        }

        public Task LoadAsync()
        {
            return Task.FromResult(0);
        }

        public void Remove(string key)
        {
            _store.Remove(key);
        }

        public void Set(string key, byte[] value)
        {
            _store[key] = value;
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            return _store.TryGetValue(key, out value);
        }
    }
}
