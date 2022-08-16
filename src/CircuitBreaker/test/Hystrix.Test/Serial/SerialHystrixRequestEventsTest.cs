// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Metric;
using Steeltoe.Common.Util;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.Serial.Test;

public class SerialHystrixRequestEventsTest
{
    private static readonly IHystrixCommandGroupKey GroupKey = HystrixCommandGroupKeyDefault.AsKey("GROUP");
    private static readonly IHystrixThreadPoolKey HystrixThreadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool");
    private static readonly IHystrixCommandKey FooKey = HystrixCommandKeyDefault.AsKey("Foo");
    private static readonly IHystrixCommandKey BarKey = HystrixCommandKeyDefault.AsKey("Bar");
    private static readonly IHystrixCollapserKey CollapserKey = HystrixCollapserKeyDefault.AsKey("FooCollapser");

    [Fact]
    public void TestEmpty()
    {
        var request = new HystrixRequestEvents(new List<IHystrixInvokableInfo>());
        string actual = SerialHystrixRequestEvents.ToJsonString(request);
        Assert.Equal("[]", actual);
    }

    [Fact]
    public void TestSingleSuccess()
    {
        var executions = new List<IHystrixInvokableInfo>
        {
            new SimpleExecution(FooKey, 100, HystrixEventType.Success)
        };

        var request = new HystrixRequestEvents(executions);
        string actual = SerialHystrixRequestEvents.ToJsonString(request);
        Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[100]}]", actual);
    }

    [Fact]
    public void TestSingleFailureFallbackMissing()
    {
        var executions = new List<IHystrixInvokableInfo>
        {
            new SimpleExecution(FooKey, 101, HystrixEventType.Failure, HystrixEventType.FallbackMissing)
        };

        var request = new HystrixRequestEvents(executions);
        string actual = SerialHystrixRequestEvents.ToJsonString(request);
        Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"FAILURE\",\"FALLBACK_MISSING\"],\"latencies\":[101]}]", actual);
    }

    [Fact]
    public void TestSingleFailureFallbackSuccess()
    {
        var executions = new List<IHystrixInvokableInfo>
        {
            new SimpleExecution(FooKey, 102, HystrixEventType.Failure, HystrixEventType.FallbackSuccess)
        };

        var request = new HystrixRequestEvents(executions);
        string actual = SerialHystrixRequestEvents.ToJsonString(request);
        Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"FAILURE\",\"FALLBACK_SUCCESS\"],\"latencies\":[102]}]", actual);
    }

    [Fact]
    public void TestSingleFailureFallbackRejected()
    {
        var executions = new List<IHystrixInvokableInfo>
        {
            new SimpleExecution(FooKey, 103, HystrixEventType.Failure, HystrixEventType.FallbackRejection)
        };

        var request = new HystrixRequestEvents(executions);
        string actual = SerialHystrixRequestEvents.ToJsonString(request);
        Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"FAILURE\",\"FALLBACK_REJECTION\"],\"latencies\":[103]}]", actual);
    }

    [Fact]
    public void TestSingleFailureFallbackFailure()
    {
        var executions = new List<IHystrixInvokableInfo>
        {
            new SimpleExecution(FooKey, 104, HystrixEventType.Failure, HystrixEventType.FallbackFailure)
        };

        var request = new HystrixRequestEvents(executions);
        string actual = SerialHystrixRequestEvents.ToJsonString(request);
        Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"FAILURE\",\"FALLBACK_FAILURE\"],\"latencies\":[104]}]", actual);
    }

    [Fact]
    public void TestSingleTimeoutFallbackSuccess()
    {
        var executions = new List<IHystrixInvokableInfo>
        {
            new SimpleExecution(FooKey, 105, HystrixEventType.Timeout, HystrixEventType.FallbackSuccess)
        };

        var request = new HystrixRequestEvents(executions);
        string actual = SerialHystrixRequestEvents.ToJsonString(request);
        Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"TIMEOUT\",\"FALLBACK_SUCCESS\"],\"latencies\":[105]}]", actual);
    }

    [Fact]
    public void TestSingleSemaphoreRejectedFallbackSuccess()
    {
        var executions = new List<IHystrixInvokableInfo>
        {
            new SimpleExecution(FooKey, 1, HystrixEventType.SemaphoreRejected, HystrixEventType.FallbackSuccess)
        };

        var request = new HystrixRequestEvents(executions);
        string actual = SerialHystrixRequestEvents.ToJsonString(request);
        Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SEMAPHORE_REJECTED\",\"FALLBACK_SUCCESS\"],\"latencies\":[1]}]", actual);
    }

    [Fact]
    public void TestSingleThreadPoolRejectedFallbackSuccess()
    {
        var executions = new List<IHystrixInvokableInfo>
        {
            new SimpleExecution(FooKey, 1, HystrixEventType.ThreadPoolRejected, HystrixEventType.FallbackSuccess)
        };

        var request = new HystrixRequestEvents(executions);
        string actual = SerialHystrixRequestEvents.ToJsonString(request);
        Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"THREAD_POOL_REJECTED\",\"FALLBACK_SUCCESS\"],\"latencies\":[1]}]", actual);
    }

    [Fact]
    public void TestSingleShortCircuitedFallbackSuccess()
    {
        var executions = new List<IHystrixInvokableInfo>
        {
            new SimpleExecution(FooKey, 1, HystrixEventType.ShortCircuited, HystrixEventType.FallbackSuccess)
        };

        var request = new HystrixRequestEvents(executions);
        string actual = SerialHystrixRequestEvents.ToJsonString(request);
        Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SHORT_CIRCUITED\",\"FALLBACK_SUCCESS\"],\"latencies\":[1]}]", actual);
    }

    [Fact]
    public void TestSingleBadRequest()
    {
        var executions = new List<IHystrixInvokableInfo>
        {
            new SimpleExecution(FooKey, 50, HystrixEventType.BadRequest)
        };

        var request = new HystrixRequestEvents(executions);
        string actual = SerialHystrixRequestEvents.ToJsonString(request);
        Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"BAD_REQUEST\"],\"latencies\":[50]}]", actual);
    }

    [Fact]
    public void TestTwoSuccessesSameKey()
    {
        var executions = new List<IHystrixInvokableInfo>();
        var foo1 = new SimpleExecution(FooKey, 23, HystrixEventType.Success);
        var foo2 = new SimpleExecution(FooKey, 34, HystrixEventType.Success);
        executions.Add(foo1);
        executions.Add(foo2);
        var request = new HystrixRequestEvents(executions);
        string actual = SerialHystrixRequestEvents.ToJsonString(request);
        Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[23,34]}]", actual);
    }

    [Fact]
    public void TestTwoSuccessesDifferentKey()
    {
        var executions = new List<IHystrixInvokableInfo>();
        var foo1 = new SimpleExecution(FooKey, 23, HystrixEventType.Success);
        var bar1 = new SimpleExecution(BarKey, 34, HystrixEventType.Success);
        executions.Add(foo1);
        executions.Add(bar1);
        var request = new HystrixRequestEvents(executions);
        string actual = SerialHystrixRequestEvents.ToJsonString(request);

        Assert.True(
            actual.Equals("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[23]},{\"name\":\"Bar\",\"events\":[\"SUCCESS\"],\"latencies\":[34]}]") ||
            actual.Equals("[{\"name\":\"Bar\",\"events\":[\"SUCCESS\"],\"latencies\":[34]},{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[23]}]"));
    }

    [Fact]
    public void TestTwoFailuresSameKey()
    {
        var executions = new List<IHystrixInvokableInfo>();
        var foo1 = new SimpleExecution(FooKey, 56, HystrixEventType.Failure, HystrixEventType.FallbackSuccess);
        var foo2 = new SimpleExecution(FooKey, 67, HystrixEventType.Failure, HystrixEventType.FallbackSuccess);
        executions.Add(foo1);
        executions.Add(foo2);
        var request = new HystrixRequestEvents(executions);
        string actual = SerialHystrixRequestEvents.ToJsonString(request);
        Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"FAILURE\",\"FALLBACK_SUCCESS\"],\"latencies\":[56,67]}]", actual);
    }

    [Fact]
    public void TestTwoSuccessesOneFailureSameKey()
    {
        var executions = new List<IHystrixInvokableInfo>();
        var foo1 = new SimpleExecution(FooKey, 10, HystrixEventType.Success);
        var foo2 = new SimpleExecution(FooKey, 67, HystrixEventType.Failure, HystrixEventType.FallbackSuccess);
        var foo3 = new SimpleExecution(FooKey, 11, HystrixEventType.Success);
        executions.Add(foo1);
        executions.Add(foo2);
        executions.Add(foo3);
        var request = new HystrixRequestEvents(executions);
        string actual = SerialHystrixRequestEvents.ToJsonString(request);

        Assert.True(
            actual.Equals(
                "[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[10,11]},{\"name\":\"Foo\",\"events\":[\"FAILURE\",\"FALLBACK_SUCCESS\"],\"latencies\":[67]}]") ||
            actual.Equals(
                "[{\"name\":\"Foo\",\"events\":[\"FAILURE\",\"FALLBACK_SUCCESS\"],\"latencies\":[67]},{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[10,11]}]"));
    }

    [Fact]
    public void TestSingleResponseFromCache()
    {
        var executions = new List<IHystrixInvokableInfo>();
        var foo1 = new SimpleExecution(FooKey, 23, "cacheKeyA", HystrixEventType.Success);
        var cachedFoo1 = new SimpleExecution(FooKey, "cacheKeyA");
        executions.Add(foo1);
        executions.Add(cachedFoo1);
        var request = new HystrixRequestEvents(executions);
        string actual = SerialHystrixRequestEvents.ToJsonString(request);
        Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[23],\"cached\":1}]", actual);
    }

    [Fact]
    public void TestMultipleResponsesFromCache()
    {
        var executions = new List<IHystrixInvokableInfo>();
        var foo1 = new SimpleExecution(FooKey, 23, "cacheKeyA", HystrixEventType.Success);
        var cachedFoo1 = new SimpleExecution(FooKey, "cacheKeyA");
        var anotherCachedFoo1 = new SimpleExecution(FooKey, "cacheKeyA");
        executions.Add(foo1);
        executions.Add(cachedFoo1);
        executions.Add(anotherCachedFoo1);
        var request = new HystrixRequestEvents(executions);
        string actual = SerialHystrixRequestEvents.ToJsonString(request);
        Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[23],\"cached\":2}]", actual);
    }

    [Fact]
    public void TestMultipleCacheKeys()
    {
        var executions = new List<IHystrixInvokableInfo>();
        var foo1 = new SimpleExecution(FooKey, 23, "cacheKeyA", HystrixEventType.Success);
        var cachedFoo1 = new SimpleExecution(FooKey, "cacheKeyA");
        var foo2 = new SimpleExecution(FooKey, 67, "cacheKeyB", HystrixEventType.Success);
        var cachedFoo2 = new SimpleExecution(FooKey, "cacheKeyB");
        executions.Add(foo1);
        executions.Add(cachedFoo1);
        executions.Add(foo2);
        executions.Add(cachedFoo2);
        var request = new HystrixRequestEvents(executions);
        string actual = SerialHystrixRequestEvents.ToJsonString(request);

        Assert.True(
            actual.Equals(
                "[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[67],\"cached\":1},{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[23],\"cached\":1}]") ||
            actual.Equals(
                "[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[23],\"cached\":1},{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[67],\"cached\":1}]"));
    }

    [Fact]
    public void TestSingleSuccessMultipleEmits()
    {
        var executions = new List<IHystrixInvokableInfo>
        {
            new SimpleExecution(FooKey, 100, HystrixEventType.Emit, HystrixEventType.Emit, HystrixEventType.Emit, HystrixEventType.Success)
        };

        var request = new HystrixRequestEvents(executions);
        string actual = SerialHystrixRequestEvents.ToJsonString(request);
        Assert.Equal("[{\"name\":\"Foo\",\"events\":[{\"name\":\"EMIT\",\"count\":3},\"SUCCESS\"],\"latencies\":[100]}]", actual);
    }

    [Fact]
    public void TestSingleSuccessMultipleEmitsAndFallbackEmits()
    {
        var executions = new List<IHystrixInvokableInfo>
        {
            new SimpleExecution(FooKey, 100, HystrixEventType.Emit, HystrixEventType.Emit, HystrixEventType.Emit, HystrixEventType.Failure,
                HystrixEventType.FallbackEmit, HystrixEventType.FallbackEmit, HystrixEventType.FallbackSuccess)
        };

        var request = new HystrixRequestEvents(executions);
        string actual = SerialHystrixRequestEvents.ToJsonString(request);

        Assert.Equal(
            "[{\"name\":\"Foo\",\"events\":[{\"name\":\"EMIT\",\"count\":3},\"FAILURE\",{\"name\":\"FALLBACK_EMIT\",\"count\":2},\"FALLBACK_SUCCESS\"],\"latencies\":[100]}]",
            actual);
    }

    [Fact]
    public void TestCollapsedBatchOfOne()
    {
        var executions = new List<IHystrixInvokableInfo>
        {
            new SimpleExecution(FooKey, 53, CollapserKey, 1, HystrixEventType.Success)
        };

        var request = new HystrixRequestEvents(executions);
        string actual = SerialHystrixRequestEvents.ToJsonString(request);
        Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[53],\"collapsed\":{\"name\":\"FooCollapser\",\"count\":1}}]", actual);
    }

    [Fact]
    public void TestCollapsedBatchOfSix()
    {
        var executions = new List<IHystrixInvokableInfo>
        {
            new SimpleExecution(FooKey, 53, CollapserKey, 6, HystrixEventType.Success)
        };

        var request = new HystrixRequestEvents(executions);
        string actual = SerialHystrixRequestEvents.ToJsonString(request);
        Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[53],\"collapsed\":{\"name\":\"FooCollapser\",\"count\":6}}]", actual);
    }

    private sealed class SimpleExecution : IHystrixInvokableInfo
    {
        private readonly ExecutionResult _executionResult;

        public IHystrixCommandGroupKey CommandGroup => GroupKey;

        public IHystrixCommandKey CommandKey { get; }

        public IHystrixThreadPoolKey ThreadPoolKey => HystrixThreadPoolKey;

        public string PublicCacheKey { get; }

        public IHystrixCollapserKey OriginatingCollapserKey { get; }

        public HystrixCommandMetrics Metrics => null;

        public IHystrixCommandOptions CommandOptions => null;

        public bool IsCircuitBreakerOpen => false;

        public bool IsExecutionComplete => true;

        public bool IsExecutedInThread => false;

        public bool IsSuccessfulExecution => _executionResult.Eventcounts.Contains(HystrixEventType.Success);

        public bool IsFailedExecution => _executionResult.Eventcounts.Contains(HystrixEventType.Failure);

        public Exception FailedExecutionException => null;

        public bool IsResponseFromFallback => _executionResult.Eventcounts.Contains(HystrixEventType.FallbackSuccess);

        public bool IsResponseTimedOut => _executionResult.Eventcounts.Contains(HystrixEventType.Timeout);

        public bool IsResponseShortCircuited => _executionResult.Eventcounts.Contains(HystrixEventType.ShortCircuited);

        public bool IsResponseFromCache => _executionResult.Eventcounts.Contains(HystrixEventType.ResponseFromCache);

        public bool IsResponseRejected => _executionResult.IsResponseRejected;

        public bool IsResponseSemaphoreRejected => _executionResult.Eventcounts.Contains(HystrixEventType.SemaphoreRejected);

        public bool IsResponseThreadPoolRejected => _executionResult.Eventcounts.Contains(HystrixEventType.ThreadPoolRejected);

        public IReadOnlyList<HystrixEventType> ExecutionEvents => _executionResult.OrderedList;

        public int NumberEmissions => _executionResult.Eventcounts.GetCount(HystrixEventType.Emit);

        public int NumberFallbackEmissions => _executionResult.Eventcounts.GetCount(HystrixEventType.FallbackEmit);

        public int NumberCollapsed => _executionResult.Eventcounts.GetCount(HystrixEventType.Collapsed);

        public int ExecutionTimeInMilliseconds => _executionResult.ExecutionLatency;

        public long CommandRunStartTimeInNanoseconds => Time.CurrentTimeMillis;

        public ExecutionResult.EventCounts EventCounts => _executionResult.Eventcounts;

        public SimpleExecution(IHystrixCommandKey commandKey, int latency, params HystrixEventType[] events)
        {
            CommandKey = commandKey;
            _executionResult = ExecutionResult.From(events).SetExecutionLatency(latency);
            PublicCacheKey = null;
            OriginatingCollapserKey = null;
        }

        public SimpleExecution(IHystrixCommandKey commandKey, int latency, string cacheKey, params HystrixEventType[] events)
        {
            CommandKey = commandKey;
            _executionResult = ExecutionResult.From(events).SetExecutionLatency(latency);
            PublicCacheKey = cacheKey;
            OriginatingCollapserKey = null;
        }

        public SimpleExecution(IHystrixCommandKey commandKey, string cacheKey)
        {
            CommandKey = commandKey;
            _executionResult = ExecutionResult.From(HystrixEventType.ResponseFromCache);
            PublicCacheKey = cacheKey;
            OriginatingCollapserKey = null;
        }

        public SimpleExecution(IHystrixCommandKey commandKey, int latency, IHystrixCollapserKey collapserKey, int batchSize, params HystrixEventType[] events)
        {
            CommandKey = commandKey;
            ExecutionResult interimResult = ExecutionResult.From(events).SetExecutionLatency(latency);

            for (int i = 0; i < batchSize; i++)
            {
                interimResult = interimResult.AddEvent(HystrixEventType.Collapsed);
            }

            _executionResult = interimResult;
            PublicCacheKey = null;
            OriginatingCollapserKey = collapserKey;
        }

        public override string ToString()
        {
            return
                $"SimpleExecution{{commandKey={CommandKey.Name}, executionResult={_executionResult}, cacheKey='{PublicCacheKey}', collapserKey={OriginatingCollapserKey}}}";
        }
    }
}
