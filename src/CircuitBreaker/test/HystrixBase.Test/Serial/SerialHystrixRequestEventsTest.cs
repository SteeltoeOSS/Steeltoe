// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Metric;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.Serial.Test
{
    public class SerialHystrixRequestEventsTest
    {
        private static readonly IHystrixCommandGroupKey GroupKey = HystrixCommandGroupKeyDefault.AsKey("GROUP");
        private static readonly IHystrixThreadPoolKey ThreadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool");
        private static readonly IHystrixCommandKey FooKey = HystrixCommandKeyDefault.AsKey("Foo");
        private static readonly IHystrixCommandKey BarKey = HystrixCommandKeyDefault.AsKey("Bar");
        private static readonly IHystrixCollapserKey CollapserKey = HystrixCollapserKeyDefault.AsKey("FooCollapser");

        [Fact]
        public void TestEmpty()
        {
            var request = new HystrixRequestEvents(new List<IHystrixInvokableInfo>());
            var actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[]", actual);
        }

        [Fact]
        public void TestSingleSuccess()
        {
            var executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 100, HystrixEventType.SUCCESS)
            };
            var request = new HystrixRequestEvents(executions);
            var actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[100]}]", actual);
        }

        [Fact]
        public void TestSingleFailureFallbackMissing()
        {
            var executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 101, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_MISSING)
            };
            var request = new HystrixRequestEvents(executions);
            var actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"FAILURE\",\"FALLBACK_MISSING\"],\"latencies\":[101]}]", actual);
        }

        [Fact]
        public void TestSingleFailureFallbackSuccess()
        {
            var executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 102, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_SUCCESS)
            };
            var request = new HystrixRequestEvents(executions);
            var actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"FAILURE\",\"FALLBACK_SUCCESS\"],\"latencies\":[102]}]", actual);
        }

        [Fact]
        public void TestSingleFailureFallbackRejected()
        {
            var executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 103, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_REJECTION)
            };
            var request = new HystrixRequestEvents(executions);
            var actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"FAILURE\",\"FALLBACK_REJECTION\"],\"latencies\":[103]}]", actual);
        }

        [Fact]
        public void TestSingleFailureFallbackFailure()
        {
            var executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 104, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_FAILURE)
            };
            var request = new HystrixRequestEvents(executions);
            var actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"FAILURE\",\"FALLBACK_FAILURE\"],\"latencies\":[104]}]", actual);
        }

        [Fact]
        public void TestSingleTimeoutFallbackSuccess()
        {
            var executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 105, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_SUCCESS)
            };
            var request = new HystrixRequestEvents(executions);
            var actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"TIMEOUT\",\"FALLBACK_SUCCESS\"],\"latencies\":[105]}]", actual);
        }

        [Fact]
        public void TestSingleSemaphoreRejectedFallbackSuccess()
        {
            var executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 1, HystrixEventType.SEMAPHORE_REJECTED, HystrixEventType.FALLBACK_SUCCESS)
            };
            var request = new HystrixRequestEvents(executions);
            var actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SEMAPHORE_REJECTED\",\"FALLBACK_SUCCESS\"],\"latencies\":[1]}]", actual);
        }

        [Fact]
        public void TestSingleThreadPoolRejectedFallbackSuccess()
        {
            var executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 1, HystrixEventType.THREAD_POOL_REJECTED, HystrixEventType.FALLBACK_SUCCESS)
            };
            var request = new HystrixRequestEvents(executions);
            var actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"THREAD_POOL_REJECTED\",\"FALLBACK_SUCCESS\"],\"latencies\":[1]}]", actual);
        }

        [Fact]
        public void TestSingleShortCircuitedFallbackSuccess()
        {
            var executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 1, HystrixEventType.SHORT_CIRCUITED, HystrixEventType.FALLBACK_SUCCESS)
            };
            var request = new HystrixRequestEvents(executions);
            var actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SHORT_CIRCUITED\",\"FALLBACK_SUCCESS\"],\"latencies\":[1]}]", actual);
        }

        [Fact]
        public void TestSingleBadRequest()
        {
            var executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 50, HystrixEventType.BAD_REQUEST)
            };
            var request = new HystrixRequestEvents(executions);
            var actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"BAD_REQUEST\"],\"latencies\":[50]}]", actual);
        }

        [Fact]
        public void TestTwoSuccessesSameKey()
        {
            var executions = new List<IHystrixInvokableInfo>();
            var foo1 = new SimpleExecution(FooKey, 23, HystrixEventType.SUCCESS);
            var foo2 = new SimpleExecution(FooKey, 34, HystrixEventType.SUCCESS);
            executions.Add(foo1);
            executions.Add(foo2);
            var request = new HystrixRequestEvents(executions);
            var actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[23,34]}]", actual);
        }

        [Fact]
        public void TestTwoSuccessesDifferentKey()
        {
            var executions = new List<IHystrixInvokableInfo>();
            var foo1 = new SimpleExecution(FooKey, 23, HystrixEventType.SUCCESS);
            var bar1 = new SimpleExecution(BarKey, 34, HystrixEventType.SUCCESS);
            executions.Add(foo1);
            executions.Add(bar1);
            var request = new HystrixRequestEvents(executions);
            var actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.True(actual.Equals("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[23]},{\"name\":\"Bar\",\"events\":[\"SUCCESS\"],\"latencies\":[34]}]") ||
                    actual.Equals("[{\"name\":\"Bar\",\"events\":[\"SUCCESS\"],\"latencies\":[34]},{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[23]}]"));
        }

        [Fact]
        public void TestTwoFailuresSameKey()
        {
            var executions = new List<IHystrixInvokableInfo>();
            var foo1 = new SimpleExecution(FooKey, 56, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_SUCCESS);
            var foo2 = new SimpleExecution(FooKey, 67, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_SUCCESS);
            executions.Add(foo1);
            executions.Add(foo2);
            var request = new HystrixRequestEvents(executions);
            var actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"FAILURE\",\"FALLBACK_SUCCESS\"],\"latencies\":[56,67]}]", actual);
        }

        [Fact]
        public void TestTwoSuccessesOneFailureSameKey()
        {
            var executions = new List<IHystrixInvokableInfo>();
            var foo1 = new SimpleExecution(FooKey, 10, HystrixEventType.SUCCESS);
            var foo2 = new SimpleExecution(FooKey, 67, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_SUCCESS);
            var foo3 = new SimpleExecution(FooKey, 11, HystrixEventType.SUCCESS);
            executions.Add(foo1);
            executions.Add(foo2);
            executions.Add(foo3);
            var request = new HystrixRequestEvents(executions);
            var actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.True(actual.Equals("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[10,11]},{\"name\":\"Foo\",\"events\":[\"FAILURE\",\"FALLBACK_SUCCESS\"],\"latencies\":[67]}]") ||
                    actual.Equals("[{\"name\":\"Foo\",\"events\":[\"FAILURE\",\"FALLBACK_SUCCESS\"],\"latencies\":[67]},{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[10,11]}]"));
        }

        [Fact]
        public void TestSingleResponseFromCache()
        {
            var executions = new List<IHystrixInvokableInfo>();
            var foo1 = new SimpleExecution(FooKey, 23, "cacheKeyA", HystrixEventType.SUCCESS);
            var cachedFoo1 = new SimpleExecution(FooKey, "cacheKeyA");
            executions.Add(foo1);
            executions.Add(cachedFoo1);
            var request = new HystrixRequestEvents(executions);
            var actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[23],\"cached\":1}]", actual);
        }

        [Fact]
        public void TestMultipleResponsesFromCache()
        {
            var executions = new List<IHystrixInvokableInfo>();
            var foo1 = new SimpleExecution(FooKey, 23, "cacheKeyA", HystrixEventType.SUCCESS);
            var cachedFoo1 = new SimpleExecution(FooKey, "cacheKeyA");
            var anotherCachedFoo1 = new SimpleExecution(FooKey, "cacheKeyA");
            executions.Add(foo1);
            executions.Add(cachedFoo1);
            executions.Add(anotherCachedFoo1);
            var request = new HystrixRequestEvents(executions);
            var actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[23],\"cached\":2}]", actual);
        }

        [Fact]
        public void TestMultipleCacheKeys()
        {
            var executions = new List<IHystrixInvokableInfo>();
            var foo1 = new SimpleExecution(FooKey, 23, "cacheKeyA", HystrixEventType.SUCCESS);
            var cachedFoo1 = new SimpleExecution(FooKey, "cacheKeyA");
            var foo2 = new SimpleExecution(FooKey, 67, "cacheKeyB", HystrixEventType.SUCCESS);
            var cachedFoo2 = new SimpleExecution(FooKey, "cacheKeyB");
            executions.Add(foo1);
            executions.Add(cachedFoo1);
            executions.Add(foo2);
            executions.Add(cachedFoo2);
            var request = new HystrixRequestEvents(executions);
            var actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.True(actual.Equals("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[67],\"cached\":1},{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[23],\"cached\":1}]") ||
                    actual.Equals("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[23],\"cached\":1},{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[67],\"cached\":1}]"));
        }

        [Fact]
        public void TestSingleSuccessMultipleEmits()
        {
            var executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 100, HystrixEventType.EMIT, HystrixEventType.EMIT, HystrixEventType.EMIT, HystrixEventType.SUCCESS)
            };
            var request = new HystrixRequestEvents(executions);
            var actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[{\"name\":\"EMIT\",\"count\":3},\"SUCCESS\"],\"latencies\":[100]}]", actual);
        }

        [Fact]
        public void TestSingleSuccessMultipleEmitsAndFallbackEmits()
        {
            var executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 100, HystrixEventType.EMIT, HystrixEventType.EMIT, HystrixEventType.EMIT, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_EMIT, HystrixEventType.FALLBACK_EMIT, HystrixEventType.FALLBACK_SUCCESS)
            };
            var request = new HystrixRequestEvents(executions);
            var actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[{\"name\":\"EMIT\",\"count\":3},\"FAILURE\",{\"name\":\"FALLBACK_EMIT\",\"count\":2},\"FALLBACK_SUCCESS\"],\"latencies\":[100]}]", actual);
        }

        [Fact]
        public void TestCollapsedBatchOfOne()
        {
            var executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 53, CollapserKey, 1, HystrixEventType.SUCCESS)
            };
            var request = new HystrixRequestEvents(executions);
            var actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[53],\"collapsed\":{\"name\":\"FooCollapser\",\"count\":1}}]", actual);
        }

        [Fact]
        public void TestCollapsedBatchOfSix()
        {
            var executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 53, CollapserKey, 6, HystrixEventType.SUCCESS)
            };
            var request = new HystrixRequestEvents(executions);
            var actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[53],\"collapsed\":{\"name\":\"FooCollapser\",\"count\":6}}]", actual);
        }

        private class SimpleExecution : IHystrixInvokableInfo
        {
            private readonly ExecutionResult executionResult;

            public SimpleExecution(IHystrixCommandKey commandKey, int latency, params HystrixEventType[] events)
            {
                CommandKey = commandKey;
                executionResult = ExecutionResult.From(events).SetExecutionLatency(latency);
                PublicCacheKey = null;
                OriginatingCollapserKey = null;
            }

            public SimpleExecution(IHystrixCommandKey commandKey, int latency, string cacheKey, params HystrixEventType[] events)
            {
                CommandKey = commandKey;
                executionResult = ExecutionResult.From(events).SetExecutionLatency(latency);
                PublicCacheKey = cacheKey;
                OriginatingCollapserKey = null;
            }

            public SimpleExecution(IHystrixCommandKey commandKey, string cacheKey)
            {
                CommandKey = commandKey;
                executionResult = ExecutionResult.From(HystrixEventType.RESPONSE_FROM_CACHE);
                PublicCacheKey = cacheKey;
                OriginatingCollapserKey = null;
            }

            public SimpleExecution(IHystrixCommandKey commandKey, int latency, IHystrixCollapserKey collapserKey, int batchSize, params HystrixEventType[] events)
            {
                CommandKey = commandKey;
                var interimResult = ExecutionResult.From(events).SetExecutionLatency(latency);
                for (var i = 0; i < batchSize; i++)
                {
                    interimResult = interimResult.AddEvent(HystrixEventType.COLLAPSED);
                }

                executionResult = interimResult;
                PublicCacheKey = null;
                OriginatingCollapserKey = collapserKey;
            }

            public IHystrixCommandGroupKey CommandGroup
            {
                get { return GroupKey; }
            }

            public IHystrixCommandKey CommandKey { get; private set; }

            public IHystrixThreadPoolKey ThreadPoolKey
            {
                get { return SerialHystrixRequestEventsTest.ThreadPoolKey; }
            }

            public string PublicCacheKey { get; private set; }

            public IHystrixCollapserKey OriginatingCollapserKey { get; private set; }

            public HystrixCommandMetrics Metrics
            {
                get { return null; }
            }

            public IHystrixCommandOptions CommandOptions
            {
                get { return null; }
            }

            public bool IsCircuitBreakerOpen
            {
                get { return false; }
            }

            public bool IsExecutionComplete
            {
                get { return true; }
            }

            public bool IsExecutedInThread
            {
                get { return false; }
            }

            public bool IsSuccessfulExecution
            {
                get { return executionResult.Eventcounts.Contains(HystrixEventType.SUCCESS); }
            }

            public bool IsFailedExecution
            {
                get { return executionResult.Eventcounts.Contains(HystrixEventType.FAILURE); }
            }

            public Exception FailedExecutionException
            {
                get { return null; }
            }

            public bool IsResponseFromFallback
            {
                get { return executionResult.Eventcounts.Contains(HystrixEventType.FALLBACK_SUCCESS); }
            }

            public bool IsResponseTimedOut
            {
                get { return executionResult.Eventcounts.Contains(HystrixEventType.TIMEOUT); }
            }

            public bool IsResponseShortCircuited
            {
                get { return executionResult.Eventcounts.Contains(HystrixEventType.SHORT_CIRCUITED); }
            }

            public bool IsResponseFromCache
            {
                get { return executionResult.Eventcounts.Contains(HystrixEventType.RESPONSE_FROM_CACHE); }
            }

            public bool IsResponseRejected
            {
                get { return executionResult.IsResponseRejected; }
            }

            public bool IsResponseSemaphoreRejected
            {
                get { return executionResult.Eventcounts.Contains(HystrixEventType.SEMAPHORE_REJECTED); }
            }

            public bool IsResponseThreadPoolRejected
            {
                get { return executionResult.Eventcounts.Contains(HystrixEventType.THREAD_POOL_REJECTED); }
            }

            public List<HystrixEventType> ExecutionEvents
            {
                get { return executionResult.OrderedList; }
            }

            public int NumberEmissions
            {
                get { return executionResult.Eventcounts.GetCount(HystrixEventType.EMIT); }
            }

            public int NumberFallbackEmissions
            {
                get { return executionResult.Eventcounts.GetCount(HystrixEventType.FALLBACK_EMIT); }
            }

            public int NumberCollapsed
            {
                get { return executionResult.Eventcounts.GetCount(HystrixEventType.COLLAPSED); }
            }

            public int ExecutionTimeInMilliseconds
            {
                get { return executionResult.ExecutionLatency; }
            }

            public long CommandRunStartTimeInNanos
            {
                get { return Time.CurrentTimeMillis; }
            }

            public ExecutionResult.EventCounts EventCounts
            {
                get { return executionResult.Eventcounts; }
            }

            public override string ToString()
            {
                return
                    $"SimpleExecution{{commandKey={CommandKey.Name}, executionResult={executionResult}, cacheKey='{PublicCacheKey}', collapserKey={OriginatingCollapserKey}}}";
            }
        }
    }
}
