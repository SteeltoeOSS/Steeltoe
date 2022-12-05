// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

public class HystrixRequestCacheTest : HystrixTestBase
{
    [Fact]
    public async Task TestCache()
    {
        try
        {
            Task<string> t1 = Task.FromResult("a1");
            Task<string> t2 = Task.FromResult("a2");
            Task<string> t3 = Task.FromResult("b1");

            var cache1 = HystrixRequestCache.GetInstance(HystrixCommandKeyDefault.AsKey("command1"));
            _ = cache1.PutIfAbsent("valueA", t1);
            _ = cache1.PutIfAbsent("valueA", t2);
            _ = cache1.PutIfAbsent("valueB", t3);

            var cache2 = HystrixRequestCache.GetInstance(HystrixCommandKeyDefault.AsKey("command2"));
            Task<string> t4 = Task.FromResult("a3");
            _ = cache2.PutIfAbsent("valueA", t4);

            Assert.Equal("a1", await cache1.Get<Task<string>>("valueA"));
            Assert.Equal("b1", await cache1.Get<Task<string>>("valueB"));

            Assert.Equal("a3", await cache2.Get<Task<string>>("valueA"));
            Assert.Null(cache2.Get<Task<string>>("valueB"));
        }
        catch (Exception e)
        {
            Assert.False(true, $"Exception: {e.Message}");
        }
        finally
        {
            Context.Dispose();
        }

        Context = HystrixRequestContext.InitializeContext();

        try
        {
            // with a new context  the instance should have nothing in it
            var cache = HystrixRequestCache.GetInstance(HystrixCommandKeyDefault.AsKey("command1"));
            Assert.Null(cache.Get<Task<string>>("valueA"));
            Assert.Null(cache.Get<Task<string>>("valueB"));
        }
        finally
        {
            Context.Dispose();
        }
    }

    [Fact]
    public async Task TestCacheWithoutContext()
    {
        Context.Dispose();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            HystrixRequestCache.GetInstance(HystrixCommandKeyDefault.AsKey("command1")).Get<Task<string>>("any"));
    }

    [Fact]
    public async Task TestClearCache()
    {
        try
        {
            var cache1 = HystrixRequestCache.GetInstance(HystrixCommandKeyDefault.AsKey("command1"));
            Task<string> t1 = Task.FromResult("a1");
            _ = cache1.PutIfAbsent("valueA", t1);
            Assert.Equal("a1", await cache1.Get<Task<string>>("valueA"));
            cache1.Clear("valueA");
            Assert.Null(cache1.Get<Task<string>>("valueA"));
        }
        catch (Exception e)
        {
            Assert.False(true, $"Exception: {e.Message}");
        }
    }

    [Fact]
    public async Task TestCacheWithoutRequestContext()
    {
        Context.Dispose();

        var cache1 = HystrixRequestCache.GetInstance(HystrixCommandKeyDefault.AsKey("command1"));
        Task<string> t1 = Task.FromResult("a1");

        // this should fail, as there's no HystrixRequestContext instance to place the cache into
        await Assert.ThrowsAsync<InvalidOperationException>(() => cache1.PutIfAbsent("valueA", t1));
    }
}
