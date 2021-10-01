// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
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

        public override void Dispose()
        {
            base.Dispose();
        }

        private static readonly AtomicInteger UniqueId = new(0);

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
                executionResult2 = executionResult;
                this.executionLatency = executionLatency;
                this.fallbackExecutionResult = fallbackExecutionResult;
                this.fallbackExecutionLatency = fallbackExecutionLatency;
                this.arg = arg;
                _isFallbackUserDefined = true;
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
                var topts = new HystrixThreadPoolOptions()
                {
                    CoreSize = 10,
                    MaxQueueSize = -1,
                    ThreadPoolKey = HystrixThreadPoolKeyDefault.AsKey(groupKey.Name)
                };

                var setter = new HystrixCommandOptions()
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
                        var arg = UniqueId.Value + string.Empty;
                        return new Command(setter, HystrixEventType.SUCCESS, 0, arg, desiredFallbackEventType, 0);
                    default:
                        throw new Exception("not supported yet");
                }
            }

            public static List<Command> GetCommandsWithResponseFromCache(IHystrixCommandGroupKey groupKey, IHystrixCommandKey key)
            {
                var cmd1 = Command.From(groupKey, key, HystrixEventType.SUCCESS);
                var cmd2 = Command.From(groupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);
                var cmds = new List<Command>
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
                    Time.WaitUntil(() => { return _token.IsCancellationRequested; }, executionLatency);

                    // sw.Stop();
                    _token.ThrowIfCancellationRequested();

                    return executionResult2 switch
                    {
                        HystrixEventType.SUCCESS => 1,
                        HystrixEventType.FAILURE => throw new Exception("induced failure"),
                        HystrixEventType.BAD_REQUEST => throw new HystrixBadRequestException("induced bad request"),
                        _ => throw new Exception("unhandled HystrixEventType : " + _executionResult),
                    };
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

                return fallbackExecutionResult switch
                {
                    HystrixEventType.FALLBACK_SUCCESS => -1,
                    HystrixEventType.FALLBACK_FAILURE => throw new Exception("induced failure"),
                    HystrixEventType.FALLBACK_MISSING => throw new InvalidOperationException("fallback not defined"),
                    _ => throw new Exception("unhandled HystrixEventType : " + fallbackExecutionResult),
                };
            }

            protected override string CacheKey
            {
                get { return arg; }
            }
        }

        public class Collapser : HystrixCollapser<List<int>, int, int>
        {
            public bool CommandCreated = false;

            private readonly int arg;
            private readonly ITestOutputHelper output;

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
                var args = new List<int>();
                foreach (var collapsedReq in collapsedRequests)
                {
                    args.Add(collapsedReq.Argument);
                }

                CommandCreated = true;

                return new BatchCommand(output, args);
            }

            protected override void MapResponseToRequests(List<int> batchResponse, ICollection<ICollapsedRequest<int, int>> collapsedRequests)
            {
                foreach (var collapsedReq in collapsedRequests)
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
            private readonly List<int> args;
            private readonly ITestOutputHelper output;

            public BatchCommand(ITestOutputHelper output, List<int> args)
             : base(HystrixCommandGroupKeyDefault.AsKey("BATCH"))
            {
                this.args = args;
                this.output = output;
            }

            protected override List<int> Run()
            {
                output.WriteLine(Time.CurrentTimeMillis + " " + Thread.CurrentThread.ManagedThreadId + " : Executing batch of : " + args.Count);
                return args;
            }
        }

        protected static bool HasData(long[] eventCounts)
        {
            foreach (var eventType in HystrixEventTypeHelper.Values)
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
