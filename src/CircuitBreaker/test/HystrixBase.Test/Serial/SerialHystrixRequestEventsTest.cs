﻿// Licensed to the .NET Foundation under one or more agreements.
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
            HystrixRequestEvents request = new HystrixRequestEvents(new List<IHystrixInvokableInfo>());
            string actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[]", actual);
        }

        [Fact]
        public void TestSingleSuccess()
        {
            List<IHystrixInvokableInfo> executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 100, HystrixEventType.SUCCESS)
            };
            HystrixRequestEvents request = new HystrixRequestEvents(executions);
            string actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[100]}]", actual);
        }

        [Fact]
        public void TestSingleFailureFallbackMissing()
        {
            List<IHystrixInvokableInfo> executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 101, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_MISSING)
            };
            HystrixRequestEvents request = new HystrixRequestEvents(executions);
            string actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"FAILURE\",\"FALLBACK_MISSING\"],\"latencies\":[101]}]", actual);
        }

        [Fact]
        public void TestSingleFailureFallbackSuccess()
        {
            List<IHystrixInvokableInfo> executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 102, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_SUCCESS)
            };
            HystrixRequestEvents request = new HystrixRequestEvents(executions);
            string actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"FAILURE\",\"FALLBACK_SUCCESS\"],\"latencies\":[102]}]", actual);
        }

        [Fact]
        public void TestSingleFailureFallbackRejected()
        {
            List<IHystrixInvokableInfo> executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 103, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_REJECTION)
            };
            HystrixRequestEvents request = new HystrixRequestEvents(executions);
            string actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"FAILURE\",\"FALLBACK_REJECTION\"],\"latencies\":[103]}]", actual);
        }

        [Fact]
        public void TestSingleFailureFallbackFailure()
        {
            List<IHystrixInvokableInfo> executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 104, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_FAILURE)
            };
            HystrixRequestEvents request = new HystrixRequestEvents(executions);
            string actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"FAILURE\",\"FALLBACK_FAILURE\"],\"latencies\":[104]}]", actual);
        }

        [Fact]
        public void TestSingleTimeoutFallbackSuccess()
        {
            List<IHystrixInvokableInfo> executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 105, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_SUCCESS)
            };
            HystrixRequestEvents request = new HystrixRequestEvents(executions);
            string actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"TIMEOUT\",\"FALLBACK_SUCCESS\"],\"latencies\":[105]}]", actual);
        }

        [Fact]
        public void TestSingleSemaphoreRejectedFallbackSuccess()
        {
            List<IHystrixInvokableInfo> executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 1, HystrixEventType.SEMAPHORE_REJECTED, HystrixEventType.FALLBACK_SUCCESS)
            };
            HystrixRequestEvents request = new HystrixRequestEvents(executions);
            string actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SEMAPHORE_REJECTED\",\"FALLBACK_SUCCESS\"],\"latencies\":[1]}]", actual);
        }

        [Fact]
        public void TestSingleThreadPoolRejectedFallbackSuccess()
        {
            List<IHystrixInvokableInfo> executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 1, HystrixEventType.THREAD_POOL_REJECTED, HystrixEventType.FALLBACK_SUCCESS)
            };
            HystrixRequestEvents request = new HystrixRequestEvents(executions);
            string actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"THREAD_POOL_REJECTED\",\"FALLBACK_SUCCESS\"],\"latencies\":[1]}]", actual);
        }

        [Fact]
        public void TestSingleShortCircuitedFallbackSuccess()
        {
            List<IHystrixInvokableInfo> executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 1, HystrixEventType.SHORT_CIRCUITED, HystrixEventType.FALLBACK_SUCCESS)
            };
            HystrixRequestEvents request = new HystrixRequestEvents(executions);
            string actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SHORT_CIRCUITED\",\"FALLBACK_SUCCESS\"],\"latencies\":[1]}]", actual);
        }

        [Fact]
        public void TestSingleBadRequest()
        {
            List<IHystrixInvokableInfo> executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 50, HystrixEventType.BAD_REQUEST)
            };
            HystrixRequestEvents request = new HystrixRequestEvents(executions);
            string actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"BAD_REQUEST\"],\"latencies\":[50]}]", actual);
        }

        [Fact]
        public void TestTwoSuccessesSameKey()
        {
            List<IHystrixInvokableInfo> executions = new List<IHystrixInvokableInfo>();
            SimpleExecution foo1 = new SimpleExecution(FooKey, 23, HystrixEventType.SUCCESS);
            SimpleExecution foo2 = new SimpleExecution(FooKey, 34, HystrixEventType.SUCCESS);
            executions.Add(foo1);
            executions.Add(foo2);
            HystrixRequestEvents request = new HystrixRequestEvents(executions);
            string actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[23,34]}]", actual);
        }

        [Fact]
        public void TestTwoSuccessesDifferentKey()
        {
            List<IHystrixInvokableInfo> executions = new List<IHystrixInvokableInfo>();
            SimpleExecution foo1 = new SimpleExecution(FooKey, 23, HystrixEventType.SUCCESS);
            SimpleExecution bar1 = new SimpleExecution(BarKey, 34, HystrixEventType.SUCCESS);
            executions.Add(foo1);
            executions.Add(bar1);
            HystrixRequestEvents request = new HystrixRequestEvents(executions);
            string actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.True(actual.Equals("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[23]},{\"name\":\"Bar\",\"events\":[\"SUCCESS\"],\"latencies\":[34]}]") ||
                    actual.Equals("[{\"name\":\"Bar\",\"events\":[\"SUCCESS\"],\"latencies\":[34]},{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[23]}]"));
        }

        [Fact]
        public void TestTwoFailuresSameKey()
        {
            List<IHystrixInvokableInfo> executions = new List<IHystrixInvokableInfo>();
            SimpleExecution foo1 = new SimpleExecution(FooKey, 56, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_SUCCESS);
            SimpleExecution foo2 = new SimpleExecution(FooKey, 67, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_SUCCESS);
            executions.Add(foo1);
            executions.Add(foo2);
            HystrixRequestEvents request = new HystrixRequestEvents(executions);
            string actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"FAILURE\",\"FALLBACK_SUCCESS\"],\"latencies\":[56,67]}]", actual);
        }

        [Fact]
        public void TestTwoSuccessesOneFailureSameKey()
        {
            List<IHystrixInvokableInfo> executions = new List<IHystrixInvokableInfo>();
            SimpleExecution foo1 = new SimpleExecution(FooKey, 10, HystrixEventType.SUCCESS);
            SimpleExecution foo2 = new SimpleExecution(FooKey, 67, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_SUCCESS);
            SimpleExecution foo3 = new SimpleExecution(FooKey, 11, HystrixEventType.SUCCESS);
            executions.Add(foo1);
            executions.Add(foo2);
            executions.Add(foo3);
            HystrixRequestEvents request = new HystrixRequestEvents(executions);
            string actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.True(actual.Equals("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[10,11]},{\"name\":\"Foo\",\"events\":[\"FAILURE\",\"FALLBACK_SUCCESS\"],\"latencies\":[67]}]") ||
                    actual.Equals("[{\"name\":\"Foo\",\"events\":[\"FAILURE\",\"FALLBACK_SUCCESS\"],\"latencies\":[67]},{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[10,11]}]"));
        }

        [Fact]
        public void TestSingleResponseFromCache()
        {
            List<IHystrixInvokableInfo> executions = new List<IHystrixInvokableInfo>();
            SimpleExecution foo1 = new SimpleExecution(FooKey, 23, "cacheKeyA", HystrixEventType.SUCCESS);
            SimpleExecution cachedFoo1 = new SimpleExecution(FooKey, "cacheKeyA");
            executions.Add(foo1);
            executions.Add(cachedFoo1);
            HystrixRequestEvents request = new HystrixRequestEvents(executions);
            string actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[23],\"cached\":1}]", actual);
        }

        [Fact]
        public void TestMultipleResponsesFromCache()
        {
            List<IHystrixInvokableInfo> executions = new List<IHystrixInvokableInfo>();
            SimpleExecution foo1 = new SimpleExecution(FooKey, 23, "cacheKeyA", HystrixEventType.SUCCESS);
            SimpleExecution cachedFoo1 = new SimpleExecution(FooKey, "cacheKeyA");
            SimpleExecution anotherCachedFoo1 = new SimpleExecution(FooKey, "cacheKeyA");
            executions.Add(foo1);
            executions.Add(cachedFoo1);
            executions.Add(anotherCachedFoo1);
            HystrixRequestEvents request = new HystrixRequestEvents(executions);
            string actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[23],\"cached\":2}]", actual);
        }

        [Fact]
        public void TestMultipleCacheKeys()
        {
            List<IHystrixInvokableInfo> executions = new List<IHystrixInvokableInfo>();
            SimpleExecution foo1 = new SimpleExecution(FooKey, 23, "cacheKeyA", HystrixEventType.SUCCESS);
            SimpleExecution cachedFoo1 = new SimpleExecution(FooKey, "cacheKeyA");
            SimpleExecution foo2 = new SimpleExecution(FooKey, 67, "cacheKeyB", HystrixEventType.SUCCESS);
            SimpleExecution cachedFoo2 = new SimpleExecution(FooKey, "cacheKeyB");
            executions.Add(foo1);
            executions.Add(cachedFoo1);
            executions.Add(foo2);
            executions.Add(cachedFoo2);
            HystrixRequestEvents request = new HystrixRequestEvents(executions);
            string actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.True(actual.Equals("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[67],\"cached\":1},{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[23],\"cached\":1}]") ||
                    actual.Equals("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[23],\"cached\":1},{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[67],\"cached\":1}]"));
        }

        [Fact]
        public void TestSingleSuccessMultipleEmits()
        {
            List<IHystrixInvokableInfo> executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 100, HystrixEventType.EMIT, HystrixEventType.EMIT, HystrixEventType.EMIT, HystrixEventType.SUCCESS)
            };
            HystrixRequestEvents request = new HystrixRequestEvents(executions);
            string actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[{\"name\":\"EMIT\",\"count\":3},\"SUCCESS\"],\"latencies\":[100]}]", actual);
        }

        [Fact]
        public void TestSingleSuccessMultipleEmitsAndFallbackEmits()
        {
            List<IHystrixInvokableInfo> executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 100, HystrixEventType.EMIT, HystrixEventType.EMIT, HystrixEventType.EMIT, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_EMIT, HystrixEventType.FALLBACK_EMIT, HystrixEventType.FALLBACK_SUCCESS)
            };
            HystrixRequestEvents request = new HystrixRequestEvents(executions);
            string actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[{\"name\":\"EMIT\",\"count\":3},\"FAILURE\",{\"name\":\"FALLBACK_EMIT\",\"count\":2},\"FALLBACK_SUCCESS\"],\"latencies\":[100]}]", actual);
        }

        [Fact]
        public void TestCollapsedBatchOfOne()
        {
            List<IHystrixInvokableInfo> executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 53, CollapserKey, 1, HystrixEventType.SUCCESS)
            };
            HystrixRequestEvents request = new HystrixRequestEvents(executions);
            string actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[53],\"collapsed\":{\"name\":\"FooCollapser\",\"count\":1}}]", actual);
        }

        [Fact]
        public void TestCollapsedBatchOfSix()
        {
            List<IHystrixInvokableInfo> executions = new List<IHystrixInvokableInfo>
            {
                new SimpleExecution(FooKey, 53, CollapserKey, 6, HystrixEventType.SUCCESS)
            };
            HystrixRequestEvents request = new HystrixRequestEvents(executions);
            string actual = SerialHystrixRequestEvents.ToJsonString(request);
            Assert.Equal("[{\"name\":\"Foo\",\"events\":[\"SUCCESS\"],\"latencies\":[53],\"collapsed\":{\"name\":\"FooCollapser\",\"count\":6}}]", actual);
        }

        private class SimpleExecution : IHystrixInvokableInfo
        {
            private readonly IHystrixCommandKey commandKey;
            private readonly ExecutionResult executionResult;
            private readonly string cacheKey;
            private readonly IHystrixCollapserKey collapserKey;

            public SimpleExecution(IHystrixCommandKey commandKey, int latency, params HystrixEventType[] events)
            {
                this.commandKey = commandKey;
                this.executionResult = ExecutionResult.From(events).SetExecutionLatency(latency);
                this.cacheKey = null;
                this.collapserKey = null;
            }

            public SimpleExecution(IHystrixCommandKey commandKey, int latency, string cacheKey, params HystrixEventType[] events)
            {
                this.commandKey = commandKey;
                this.executionResult = ExecutionResult.From(events).SetExecutionLatency(latency);
                this.cacheKey = cacheKey;
                this.collapserKey = null;
            }

            public SimpleExecution(IHystrixCommandKey commandKey, string cacheKey)
            {
                this.commandKey = commandKey;
                this.executionResult = ExecutionResult.From(HystrixEventType.RESPONSE_FROM_CACHE);
                this.cacheKey = cacheKey;
                this.collapserKey = null;
            }

            public SimpleExecution(IHystrixCommandKey commandKey, int latency, IHystrixCollapserKey collapserKey, int batchSize, params HystrixEventType[] events)
            {
                this.commandKey = commandKey;
                ExecutionResult interimResult = ExecutionResult.From(events).SetExecutionLatency(latency);
                for (int i = 0; i < batchSize; i++)
                {
                    interimResult = interimResult.AddEvent(HystrixEventType.COLLAPSED);
                }

                this.executionResult = interimResult;
                this.cacheKey = null;
                this.collapserKey = collapserKey;
            }

            public IHystrixCommandGroupKey CommandGroup
            {
                get { return GroupKey; }
            }

            public IHystrixCommandKey CommandKey
            {
                get { return commandKey; }
            }

            public IHystrixThreadPoolKey ThreadPoolKey
            {
                get { return SerialHystrixRequestEventsTest.ThreadPoolKey; }
            }

            public string PublicCacheKey
            {
                get { return cacheKey; }
            }

            public IHystrixCollapserKey OriginatingCollapserKey
            {
                get { return collapserKey; }
            }

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
                return "SimpleExecution{" +
                        "commandKey=" + commandKey.Name +
                        ", executionResult=" + executionResult +
                        ", cacheKey='" + cacheKey + '\'' +
                        ", collapserKey=" + collapserKey +
                        '}';
            }
        }
    }
}
