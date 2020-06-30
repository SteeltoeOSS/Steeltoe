// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class HystrixRequestContextMiddlewareTest
    {
        [Fact]
        public async void Invoke_CreatesContext_ThenDisposes()
        {
            RequestDelegate del = (ctx) =>
            {
                Assert.True(HystrixRequestContext.IsCurrentThreadInitialized);
                return Task.FromResult<int>(1);
            };
            var life = new TestLifecyecle();
            var reqContext = new HystrixRequestContextMiddleware(del, life);
            HttpContext context = new DefaultHttpContext();
            await reqContext.Invoke(context);
            Assert.False(HystrixRequestContext.IsCurrentThreadInitialized);
            life.StopApplication();
        }

        [Fact]
        public void HystrixRequestContextMiddleware_RegistersStoppingAction()
        {
            RequestDelegate del = (ctx) =>
            {
                Assert.True(HystrixRequestContext.IsCurrentThreadInitialized);
                return Task.FromResult<int>(1);
            };
            var life = new TestLifecyecle();
            var reqContext = new HystrixRequestContextMiddleware(del, life);
            Assert.True(life.Registered);
            life.StopApplication();
        }

#pragma warning disable CS0618 // Type or member is obsolete
        private class TestLifecyecle : IApplicationLifetime
#pragma warning restore CS0618 // Type or member is obsolete
        {
            public bool Registered = false;

            private readonly CancellationTokenSource _stoppingSource = new CancellationTokenSource();

            public CancellationToken ApplicationStarted => throw new System.NotImplementedException();

            public CancellationToken ApplicationStopping
            {
                get
                {
                    Registered = true;
                    return _stoppingSource.Token;
                }
            }

            public CancellationToken ApplicationStopped => throw new System.NotImplementedException();

            public void StopApplication()
            {
                _stoppingSource.Cancel();
            }
        }
    }
}
