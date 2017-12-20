// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class HystrixRequestCacheTest : HystrixTestBase
    {
        private ITestOutputHelper output;

        public HystrixRequestCacheTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;
        }

        [Fact]
        public void TestCache()
        {
            try
            {
                Task<string> t1 = Task.FromResult("a1");
                Task<string> t2 = Task.FromResult("a2");
                Task<string> t3 = Task.FromResult("b1");

                HystrixRequestCache cache1 = HystrixRequestCache.GetInstance(HystrixCommandKeyDefault.AsKey("command1"));
                cache1.PutIfAbsent("valueA", t1);
                cache1.PutIfAbsent("valueA", t2);
                cache1.PutIfAbsent("valueB", t3);

                HystrixRequestCache cache2 = HystrixRequestCache.GetInstance(HystrixCommandKeyDefault.AsKey("command2"));
                Task<string> t4 = Task.FromResult("a3");
                cache2.PutIfAbsent("valueA", t4);

                Assert.Equal("a1", cache1.Get<Task<string>>("valueA").Result);
                Assert.Equal("b1", cache1.Get<Task<string>>("valueB").Result);

                Assert.Equal("a3", cache2.Get<Task<string>>("valueA").Result);
                Assert.Null(cache2.Get<Task<string>>("valueB"));
            }
            catch (Exception e)
            {
                Assert.False(true, "Exception: " + e.Message);
                output.WriteLine(e.ToString());
            }
            finally
            {
                context.Dispose();
            }

            context = HystrixRequestContext.InitializeContext();
            try
            {
                // with a new context  the instance should have nothing in it
                HystrixRequestCache cache = HystrixRequestCache.GetInstance(HystrixCommandKeyDefault.AsKey("command1"));
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
            this.context.Dispose();

            Assert.Throws<InvalidOperationException>(() => { HystrixRequestCache.GetInstance(HystrixCommandKeyDefault.AsKey("command1")).Get<Task<string>>("any");  });
        }

        [Fact]
    public void TestClearCache()
        {
            HystrixConcurrencyStrategy strategy = HystrixConcurrencyStrategyDefault.GetInstance();
            try
            {
                HystrixRequestCache cache1 = HystrixRequestCache.GetInstance(HystrixCommandKeyDefault.AsKey("command1"));
                Task<string> t1 = Task.FromResult("a1");
                cache1.PutIfAbsent("valueA", t1);
                Assert.Equal("a1", cache1.Get<Task<string>>("valueA").Result);
                cache1.Clear("valueA");
                Assert.Null(cache1.Get<Task<string>>("valueA"));
            }
            catch (Exception e)
            {
                Assert.False(true, "Exception: " + e.Message);
                output.WriteLine(e.ToString());
            }
        }

        [Fact]
        public void TestCacheWithoutRequestContext()
        {
            HystrixConcurrencyStrategy strategy = HystrixConcurrencyStrategyDefault.GetInstance();
            context.Dispose();

            HystrixRequestCache cache1 = HystrixRequestCache.GetInstance(HystrixCommandKeyDefault.AsKey("command1"));
            Task<string> t1 = Task.FromResult("a1");

            // this should fail, as there's no HystrixRequestContext instance to place the cache into
            Assert.Throws<InvalidOperationException>(() => { cache1.PutIfAbsent("valueA", t1); });
        }
    }
}
