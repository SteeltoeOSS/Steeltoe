// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Http;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class HystrixRequestContextMiddlewareTest
    {
        [Fact]
        public async void Invoke_CreatesContext_ThenDisposes()
        {
            static Task Del(HttpContext ctx)
            {
                Assert.True(HystrixRequestContext.IsCurrentThreadInitialized);
                return Task.FromResult<int>(1);
            }

            var reqContext = new HystrixRequestContextMiddleware(Del);
            HttpContext context = new DefaultHttpContext();
            await reqContext.Invoke(context);
            Assert.False(HystrixRequestContext.IsCurrentThreadInitialized);
        }
    }
}
