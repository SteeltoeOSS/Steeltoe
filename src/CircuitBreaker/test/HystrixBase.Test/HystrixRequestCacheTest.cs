// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class HystrixRequestCacheTest : HystrixTestBase
    {
        private readonly ITestOutputHelper output;

        public HystrixRequestCacheTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void TestCache()
        {
            try
            {
                var t1 = Task.FromResult("a1");
                var t2 = Task.FromResult("a2");
                var t3 = Task.FromResult("b1");

                var cache1 = HystrixRequestCache.GetInstance(HystrixCommandKeyDefault.AsKey("command1"));
                cache1.PutIfAbsent("valueA", t1);
                cache1.PutIfAbsent("valueA", t2);
                cache1.PutIfAbsent("valueB", t3);

                var cache2 = HystrixRequestCache.GetInstance(HystrixCommandKeyDefault.AsKey("command2"));
                var t4 = Task.FromResult("a3");
                cache2.PutIfAbsent("valueA", t4);

                Assert.Equal("a1", cache1.Get<Task<string>>("valueA").GetAwaiter().GetResult());
                Assert.Equal("b1", cache1.Get<Task<string>>("valueB").GetAwaiter().GetResult());

                Assert.Equal("a3", cache2.Get<Task<string>>("valueA").GetAwaiter().GetResult());
                Assert.Null(cache2.Get<Task<string>>("valueB"));
            }
            catch (Exception e)
            {
                Assert.False(true, $"Exception: {e.Message}");
            }
            finally
            {
                context.Dispose();
            }

            context = HystrixRequestContext.InitializeContext();
            try
            {
                // with a new context  the instance should have nothing in it
                var cache = HystrixRequestCache.GetInstance(HystrixCommandKeyDefault.AsKey("command1"));
                Assert.Null(cache.Get<Task<string>>("valueA"));
                Assert.Null(cache.Get<Task<string>>("valueB"));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Fact]
        public void TestCacheWithoutContext()
        {
            context.Dispose();

            Assert.Throws<InvalidOperationException>(() => { HystrixRequestCache.GetInstance(HystrixCommandKeyDefault.AsKey("command1")).Get<Task<string>>("any"); });
        }

        [Fact]
        public void TestClearCache()
        {
            var strategy = HystrixConcurrencyStrategyDefault.GetInstance();
            try
            {
                var cache1 = HystrixRequestCache.GetInstance(HystrixCommandKeyDefault.AsKey("command1"));
                var t1 = Task.FromResult("a1");
                cache1.PutIfAbsent("valueA", t1);
                Assert.Equal("a1", cache1.Get<Task<string>>("valueA").GetAwaiter().GetResult());
                cache1.Clear("valueA");
                Assert.Null(cache1.Get<Task<string>>("valueA"));
            }
            catch (Exception e)
            {
                Assert.False(true, $"Exception: {e.Message}");
            }
        }

        [Fact]
        public void TestCacheWithoutRequestContext()
        {
            var strategy = HystrixConcurrencyStrategyDefault.GetInstance();
            context.Dispose();

            var cache1 = HystrixRequestCache.GetInstance(HystrixCommandKeyDefault.AsKey("command1"));
            var t1 = Task.FromResult("a1");

            // this should fail, as there's no HystrixRequestContext instance to place the cache into
            Assert.Throws<InvalidOperationException>(() => { cache1.PutIfAbsent("valueA", t1); });
        }
    }
}
