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

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Test
{
    public abstract class CommandStreamTest : HystrixTestBase
    {
        public CommandStreamTest()
            : base()
        {
        }

        private static readonly AtomicInteger UniqueId = new AtomicInteger(0);

        public class Command : HystrixCommand<int>
        {
            private readonly int executionLatency;
            private readonly string arg;
            private readonly HystrixEventType executionResult2;
            private readonly HystrixEventType fallbackExecutionResult;
            private readonly int fallbackExecutionLatency;

            private Command(
                HystrixCommandOptions setter,
                HystrixEventType executionResult,
                int executionLatency,
                string arg,
                HystrixEventType fallbackExecutionResult,
                int fallbackExecutionLatency)
                : base(setter)
            {
                this.executionResult2 = executionResult;
                this.executionLatency = executionLatency;
                this.fallbackExecutionResult = fallbackExecutionResult;
                this.fallbackExecutionLatency = fallbackExecutionLatency;
                this.arg = arg;
                this._isFallbackUserDefined = true;
            }

            public static Command From(IHystrixCommandGroupKey groupKey, IHystrixCommandKey key, HystrixEventType desiredEventType)
            {
                return From(groupKey, key, desiredEventType, 0);
            }

            public static Command From(IHystrixCommandGroupKey groupKey, IHystrixCommandKey key, HystrixEventType desiredEventType, int latency)
            {
                return From(groupKey, key, desiredEventType, latency, ExecutionIsolationStrategy.THREAD);
            }

            public static Command From(
                IHystrixCommandGroupKey groupKey,
                IHystrixCommandKey key,
                HystrixEventType desiredEventType,
                int latency,
                HystrixEventType desiredFallbackEventType)
            {
                return From(groupKey, key, desiredEventType, latency, ExecutionIsolationStrategy.THREAD, desiredFallbackEventType);
            }

            public static Command From(
                IHystrixCommandGroupKey groupKey,
                IHystrixCommandKey key,
                HystrixEventType desiredEventType,
                int latency,
                HystrixEventType desiredFallbackEventType,
                int fallbackLatency)
            {
                return From(groupKey, key, desiredEventType, latency, ExecutionIsolationStrategy.THREAD, desiredFallbackEventType, fallbackLatency);
            }

            public static Command From(
                IHystrixCommandGroupKey groupKey,
                IHystrixCommandKey key,
                HystrixEventType desiredEventType,
                int latency,
                ExecutionIsolationStrategy isolationStrategy)
            {
                return From(groupKey, key, desiredEventType, latency, isolationStrategy, HystrixEventType.FALLBACK_SUCCESS, 0);
            }

            public static Command From(
                IHystrixCommandGroupKey groupKey,
                IHystrixCommandKey key,
                HystrixEventType desiredEventType,
                int latency,
                ExecutionIsolationStrategy isolationStrategy,
                HystrixEventType desiredFallbackEventType)
            {
                return From(groupKey, key, desiredEventType, latency, isolationStrategy, desiredFallbackEventType, 0);
            }

            public static Command From(
                IHystrixCommandGroupKey groupKey,
                IHystrixCommandKey key,
                HystrixEventType desiredEventType,
                int latency,
                ExecutionIsolationStrategy isolationStrategy,
                HystrixEventType desiredFallbackEventType,
                int fallbackLatency)
            {
                HystrixThreadPoolOptions topts = new HystrixThreadPoolOptions()
                {
                    CoreSize = 10,
                    MaxQueueSize = -1,
                    ThreadPoolKey = HystrixThreadPoolKeyDefault.AsKey(groupKey.Name)
                };

                HystrixCommandOptions setter = new HystrixCommandOptions()
                {
                    GroupKey = groupKey,
                    CommandKey = key,

                    ExecutionTimeoutInMilliseconds = 600,
                    ExecutionIsolationStrategy = isolationStrategy,
                    CircuitBreakerEnabled = true,
                    CircuitBreakerRequestVolumeThreshold = 3,
                    MetricsHealthSnapshotIntervalInMilliseconds = 100,
                    MetricsRollingStatisticalWindowInMilliseconds = 1000,
                    MetricsRollingStatisticalWindowBuckets = 10,
                    RequestCacheEnabled = true,
                    RequestLogEnabled = true,
                    FallbackIsolationSemaphoreMaxConcurrentRequests = 5,
                    ThreadPoolKey = HystrixThreadPoolKeyDefault.AsKey(groupKey.Name),
                    ThreadPoolOptions = topts
                };

                string uniqueArg;

                switch (desiredEventType)
                {
                    case HystrixEventType.SUCCESS:
                        uniqueArg = UniqueId.IncrementAndGet() + string.Empty;
                        return new Command(setter, HystrixEventType.SUCCESS, latency, uniqueArg, desiredFallbackEventType, 0);
                    case HystrixEventType.FAILURE:
                        uniqueArg = UniqueId.IncrementAndGet() + string.Empty;
                        return new Command(setter, HystrixEventType.FAILURE, latency, uniqueArg, desiredFallbackEventType, fallbackLatency);
                    case HystrixEventType.TIMEOUT:
                        uniqueArg = UniqueId.IncrementAndGet() + string.Empty;
                        return new Command(setter, HystrixEventType.SUCCESS, 1000, uniqueArg, desiredFallbackEventType, fallbackLatency); // use 1000 so that it always times out (at 600ms)
                    case HystrixEventType.BAD_REQUEST:
                        uniqueArg = UniqueId.IncrementAndGet() + string.Empty;
                        return new Command(setter, HystrixEventType.BAD_REQUEST, latency, uniqueArg, desiredFallbackEventType, 0);
                    case HystrixEventType.RESPONSE_FROM_CACHE:
                        string arg = UniqueId.Value + string.Empty;
                        return new Command(setter, HystrixEventType.SUCCESS, 0, arg, desiredFallbackEventType, 0);
                    default:
                        throw new Exception("not supported yet");
                }
            }

            public static List<Command> GetCommandsWithResponseFromCache(IHystrixCommandGroupKey groupKey, IHystrixCommandKey key)
            {
                Command cmd1 = Command.From(groupKey, key, HystrixEventType.SUCCESS);
                Command cmd2 = Command.From(groupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);
                List<Command> cmds = new List<Command>
                {
                    cmd1,
                    cmd2
                };
                return cmds;
            }

            // public Stopwatch sw = new Stopwatch();
            protected override int Run()
            {
                try
                {
                    // sw.Start();
                    Time.WaitUntil(() => { return this._token.IsCancellationRequested; }, executionLatency);

                    // sw.Stop();
                    this._token.ThrowIfCancellationRequested();

                    switch (executionResult2)
                    {
                        case HystrixEventType.SUCCESS:
                            return 1;
                        case HystrixEventType.FAILURE:
                            throw new Exception("induced failure");
                        case HystrixEventType.BAD_REQUEST:
                            throw new HystrixBadRequestException("induced bad request");
                        default:
                            throw new Exception("unhandled HystrixEventType : " + _executionResult);
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }

            protected override int RunFallback()
            {
                try
                {
                    Time.Wait(fallbackExecutionLatency);
                }
                catch (Exception)
                {
                    throw;
                }

                switch (fallbackExecutionResult)
                {
                    case HystrixEventType.FALLBACK_SUCCESS: return -1;
                    case HystrixEventType.FALLBACK_FAILURE: throw new Exception("induced failure");
                    case HystrixEventType.FALLBACK_MISSING: throw new InvalidOperationException("fallback not defined");
                    default: throw new Exception("unhandled HystrixEventType : " + fallbackExecutionResult);
                }
            }

            protected override string CacheKey
            {
                get { return arg; }
            }
        }

        public class Collapser : HystrixCollapser<List<int>, int, int>
        {
            private readonly int arg;
            private ITestOutputHelper output;

            public static Collapser From(ITestOutputHelper output, int arg)
            {
                return new Collapser(output, HystrixCollapserKeyDefault.AsKey("Collapser"), arg);
            }

            public static Collapser From(ITestOutputHelper output, IHystrixCollapserKey key, int arg)
            {
                return new Collapser(output, key, arg);
            }

            private Collapser(ITestOutputHelper output, IHystrixCollapserKey key, int arg)
                : base(Options(key, 100))
            {
                this.arg = arg;
                this.output = output;
            }

            private static HystrixCollapserOptions Options(IHystrixCollapserKey key, int timerDelay)
            {
                var opts = new HystrixCollapserOptions(key)
                {
                    TimerDelayInMilliseconds = timerDelay
                };
                return opts;
            }

            public override int RequestArgument
            {
                get { return arg; }
            }

            protected override HystrixCommand<List<int>> CreateCommand(ICollection<ICollapsedRequest<int, int>> collapsedRequests)
            {
                List<int> args = new List<int>();
                foreach (ICollapsedRequest<int, int> collapsedReq in collapsedRequests)
                {
                    args.Add(collapsedReq.Argument);
                }

                return new BatchCommand(output, args);
            }

            protected override void MapResponseToRequests(List<int> batchResponse, ICollection<ICollapsedRequest<int, int>> collapsedRequests)
            {
                foreach (ICollapsedRequest<int, int> collapsedReq in collapsedRequests)
                {
                    collapsedReq.Response = collapsedReq.Argument;
                    collapsedReq.Complete = true;
                }
            }

            protected override string CacheKey
            {
                get { return arg.ToString(); }
            }
        }

        internal class BatchCommand : HystrixCommand<List<int>>
        {
            private List<int> args;
            private ITestOutputHelper output;

            public BatchCommand(ITestOutputHelper output, List<int> args)
             : base(HystrixCommandGroupKeyDefault.AsKey("BATCH"))
            {
                this.args = args;
                this.output = output;
            }

            protected override List<int> Run()
            {
                output.WriteLine((DateTime.Now.Ticks / 10000) + " " + Thread.CurrentThread.ManagedThreadId + " : Executing batch of : " + args.Count);
                return args;
            }
        }

        protected static string BucketToString(long[] eventCounts)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            foreach (HystrixEventType eventType in HystrixEventTypeHelper.Values)
            {
                if (eventCounts[(int)eventType] > 0)
                {
                    sb.Append(eventType).Append("->").Append(eventCounts[(int)eventType]).Append(", ");
                }
            }

            sb.Append("]");
            return sb.ToString();
        }

        protected static bool HasData(long[] eventCounts)
        {
            foreach (HystrixEventType eventType in HystrixEventTypeHelper.Values)
            {
                if (eventCounts[(int)eventType] > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
