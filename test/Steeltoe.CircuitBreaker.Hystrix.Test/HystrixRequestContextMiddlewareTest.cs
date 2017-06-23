using Microsoft.AspNetCore.Http;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class HystrixRequestContextMiddlewareTest
    {
        [Fact]
        public async void Invoke_CreatesContext_ThenDisposes()
        {
            RequestDelegate del = (ctx) => {
                Assert.True(HystrixRequestContext.IsCurrentThreadInitialized);
                return Task.FromResult<int>(1);
            };
            var reqContext = new HystrixRequestContextMiddleware(del);
            HttpContext context = new DefaultHttpContext();
            await reqContext.Invoke(context);
            Assert.False(HystrixRequestContext.IsCurrentThreadInitialized);
        }
    }
}
