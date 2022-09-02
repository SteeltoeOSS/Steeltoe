// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using Steeltoe.Common.Util;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Test;

public abstract class CommandStreamTest : HystrixTestBase
{
    private static readonly AtomicInteger UniqueId = new(0);

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

    public class Command : HystrixCommand<int>
    {
        private readonly int _executionLatency;
        private readonly HystrixEventType _executionResult2;
        private readonly HystrixEventType _fallbackExecutionResult;
        private readonly int _fallbackExecutionLatency;

        protected override string CacheKey { get; }

        private Command(HystrixCommandOptions setter, HystrixEventType executionResult, int executionLatency, string arg,
            HystrixEventType fallbackExecutionResult, int fallbackExecutionLatency)
            : base(setter)
        {
            _executionResult2 = executionResult;
            _executionLatency = executionLatency;
            _fallbackExecutionResult = fallbackExecutionResult;
            _fallbackExecutionLatency = fallbackExecutionLatency;
            CacheKey = arg;
            IsFallbackUserDefined = true;
        }

        public static Command From(IHystrixCommandGroupKey groupKey, IHystrixCommandKey key, HystrixEventType desiredEventType)
        {
            return From(groupKey, key, desiredEventType, 0);
        }

        public static Command From(IHystrixCommandGroupKey groupKey, IHystrixCommandKey key, HystrixEventType desiredEventType, int latency)
        {
            return From(groupKey, key, desiredEventType, latency, ExecutionIsolationStrategy.Thread);
        }

        public static Command From(IHystrixCommandGroupKey groupKey, IHystrixCommandKey key, HystrixEventType desiredEventType, int latency,
            HystrixEventType desiredFallbackEventType)
        {
            return From(groupKey, key, desiredEventType, latency, ExecutionIsolationStrategy.Thread, desiredFallbackEventType);
        }

        public static Command From(IHystrixCommandGroupKey groupKey, IHystrixCommandKey key, HystrixEventType desiredEventType, int latency,
            HystrixEventType desiredFallbackEventType, int fallbackLatency)
        {
            return From(groupKey, key, desiredEventType, latency, ExecutionIsolationStrategy.Thread, desiredFallbackEventType, fallbackLatency);
        }

        public static Command From(IHystrixCommandGroupKey groupKey, IHystrixCommandKey key, HystrixEventType desiredEventType, int latency,
            ExecutionIsolationStrategy isolationStrategy)
        {
            return From(groupKey, key, desiredEventType, latency, isolationStrategy, HystrixEventType.FallbackSuccess, 0);
        }

        public static Command From(IHystrixCommandGroupKey groupKey, IHystrixCommandKey key, HystrixEventType desiredEventType, int latency,
            ExecutionIsolationStrategy isolationStrategy, HystrixEventType desiredFallbackEventType)
        {
            return From(groupKey, key, desiredEventType, latency, isolationStrategy, desiredFallbackEventType, 0);
        }

        public static Command From(IHystrixCommandGroupKey groupKey, IHystrixCommandKey key, HystrixEventType desiredEventType, int latency,
            ExecutionIsolationStrategy isolationStrategy, HystrixEventType desiredFallbackEventType, int fallbackLatency)
        {
            var options = new HystrixThreadPoolOptions
            {
                CoreSize = 10,
                MaxQueueSize = -1,
                ThreadPoolKey = HystrixThreadPoolKeyDefault.AsKey(groupKey.Name)
            };

            var setter = new HystrixCommandOptions
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
                ThreadPoolOptions = options
            };

            string uniqueArg;

            switch (desiredEventType)
            {
                case HystrixEventType.Success:
                    uniqueArg = UniqueId.IncrementAndGet() + string.Empty;
                    return new Command(setter, HystrixEventType.Success, latency, uniqueArg, desiredFallbackEventType, 0);
                case HystrixEventType.Failure:
                    uniqueArg = UniqueId.IncrementAndGet() + string.Empty;
                    return new Command(setter, HystrixEventType.Failure, latency, uniqueArg, desiredFallbackEventType, fallbackLatency);
                case HystrixEventType.Timeout:
                    uniqueArg = UniqueId.IncrementAndGet() + string.Empty;

                    return new Command(setter, HystrixEventType.Success, 1000, uniqueArg, desiredFallbackEventType,
                        fallbackLatency); // use 1000 so that it always times out (at 600ms)
                case HystrixEventType.BadRequest:
                    uniqueArg = UniqueId.IncrementAndGet() + string.Empty;
                    return new Command(setter, HystrixEventType.BadRequest, latency, uniqueArg, desiredFallbackEventType, 0);
                case HystrixEventType.ResponseFromCache:
                    string arg = UniqueId.Value + string.Empty;
                    return new Command(setter, HystrixEventType.Success, 0, arg, desiredFallbackEventType, 0);
                default:
                    throw new Exception("not supported yet");
            }
        }

        public static List<Command> GetCommandsWithResponseFromCache(IHystrixCommandGroupKey groupKey, IHystrixCommandKey key)
        {
            Command cmd1 = From(groupKey, key, HystrixEventType.Success);
            Command cmd2 = From(groupKey, key, HystrixEventType.ResponseFromCache);

            var commands = new List<Command>
            {
                cmd1,
                cmd2
            };

            return commands;
        }

        // public Stopwatch sw = new Stopwatch();
        protected override int Run()
        {
            // sw.Start();
            Time.WaitUntil(() => Token.IsCancellationRequested, _executionLatency);

            // sw.Stop();
            Token.ThrowIfCancellationRequested();

            return _executionResult2 switch
            {
                HystrixEventType.Success => 1,
                HystrixEventType.Failure => throw new Exception("induced failure"),
                HystrixEventType.BadRequest => throw new HystrixBadRequestException("induced bad request"),
                _ => throw new Exception($"unhandled HystrixEventType : {ExecutionResult}")
            };
        }

        protected override int RunFallback()
        {
            Time.Wait(_fallbackExecutionLatency);

            return _fallbackExecutionResult switch
            {
                HystrixEventType.FallbackSuccess => -1,
                HystrixEventType.FallbackFailure => throw new Exception("induced failure"),
                HystrixEventType.FallbackMissing => throw new InvalidOperationException("fallback not defined"),
                _ => throw new Exception($"unhandled HystrixEventType : {_fallbackExecutionResult}")
            };
        }
    }

    public class Collapser : HystrixCollapser<List<int>, int, int>
    {
        private readonly int _arg;
        private readonly ITestOutputHelper _output;

        protected override string CacheKey => _arg.ToString();
        public bool CommandCreated { get; set; }

        public override int RequestArgument => _arg;

        private Collapser(ITestOutputHelper output, IHystrixCollapserKey key, int arg)
            : base(Options(key, 100))
        {
            _arg = arg;
            _output = output;
        }

        public static Collapser From(ITestOutputHelper output, int arg)
        {
            return new Collapser(output, HystrixCollapserKeyDefault.AsKey("Collapser"), arg);
        }

        public static Collapser From(ITestOutputHelper output, IHystrixCollapserKey key, int arg)
        {
            return new Collapser(output, key, arg);
        }

        private static HystrixCollapserOptions Options(IHystrixCollapserKey key, int timerDelay)
        {
            var opts = new HystrixCollapserOptions(key)
            {
                TimerDelayInMilliseconds = timerDelay
            };

            return opts;
        }

        protected override HystrixCommand<List<int>> CreateCommand(ICollection<ICollapsedRequest<int, int>> requests)
        {
            var args = new List<int>();

            foreach (ICollapsedRequest<int, int> collapsedReq in requests)
            {
                args.Add(collapsedReq.Argument);
            }

            CommandCreated = true;

            return new BatchCommand(_output, args);
        }

        protected override void MapResponseToRequests(List<int> batchResponse, ICollection<ICollapsedRequest<int, int>> requests)
        {
            foreach (ICollapsedRequest<int, int> collapsedReq in requests)
            {
                collapsedReq.Response = collapsedReq.Argument;
                collapsedReq.Complete = true;
            }
        }
    }

    internal sealed class BatchCommand : HystrixCommand<List<int>>
    {
        private readonly List<int> _args;
        private readonly ITestOutputHelper _output;

        public BatchCommand(ITestOutputHelper output, List<int> args)
            : base(HystrixCommandGroupKeyDefault.AsKey("BATCH"))
        {
            _args = args;
            _output = output;
        }

        protected override List<int> Run()
        {
            _output.WriteLine(Time.CurrentTimeMillis + " " + Thread.CurrentThread.ManagedThreadId + " : Executing batch of : " + _args.Count);
            return _args;
        }
    }
}
