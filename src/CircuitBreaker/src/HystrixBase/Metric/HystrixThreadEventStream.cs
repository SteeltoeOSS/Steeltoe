// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric
{
    public class HystrixThreadEventStream
    {
        private static ThreadLocal<HystrixThreadEventStream> threadLocalStreams = new ThreadLocal<HystrixThreadEventStream>(
            () =>
        {
            return new HystrixThreadEventStream(Thread.CurrentThread.ManagedThreadId);
        }, true);

        private readonly long _threadId;
        private readonly string _threadName;
        private readonly ISubject<HystrixCommandExecutionStarted, HystrixCommandExecutionStarted> _writeOnlyCommandStartSubject;
        private readonly ISubject<HystrixCommandCompletion, HystrixCommandCompletion> _writeOnlyCommandCompletionSubject;
        private readonly ISubject<HystrixCollapserEvent, HystrixCollapserEvent> _writeOnlyCollapserSubject;

        private static Action<HystrixCommandExecutionStarted> WriteCommandStartsToShardedStreams { get; } = (@event) =>
        {
            var commandStartStream = HystrixCommandStartStream.GetInstance(@event.CommandKey);
            commandStartStream.Write(@event);

            if (@event.IsExecutedInThread)
            {
                var threadPoolStartStream = HystrixThreadPoolStartStream.GetInstance(@event.ThreadPoolKey);
                threadPoolStartStream.Write(@event);
            }
        };

        private static Action<HystrixCommandCompletion> WriteCommandCompletionsToShardedStreams { get; } = (commandCompletion) =>
        {
            var commandStream = HystrixCommandCompletionStream.GetInstance(commandCompletion.CommandKey);
            commandStream.Write(commandCompletion);

            if (commandCompletion.IsExecutedInThread || commandCompletion.IsResponseThreadPoolRejected)
            {
                var threadPoolStream = HystrixThreadPoolCompletionStream.GetInstance(commandCompletion.ThreadPoolKey);
                threadPoolStream.Write(commandCompletion);
            }
        };

        private static Action<HystrixCollapserEvent> WriteCollapserExecutionsToShardedStreams { get; } = (collapserEvent) =>
        {
            var collapserStream = HystrixCollapserEventStream.GetInstance(collapserEvent.CollapserKey);
            collapserStream.Write(collapserEvent);
        };

        internal HystrixThreadEventStream(int id)
        {
            _threadId = id;
            _threadName = "hystrix-" + _threadId;

            _writeOnlyCommandStartSubject = Subject.Synchronize<HystrixCommandExecutionStarted, HystrixCommandExecutionStarted>(new Subject<HystrixCommandExecutionStarted>());
            _writeOnlyCommandCompletionSubject = Subject.Synchronize<HystrixCommandCompletion, HystrixCommandCompletion>(new Subject<HystrixCommandCompletion>());
            _writeOnlyCollapserSubject = Subject.Synchronize<HystrixCollapserEvent, HystrixCollapserEvent>(new Subject<HystrixCollapserEvent>());

            _writeOnlyCommandStartSubject
                    .Do((n) => WriteCommandStartsToShardedStreams(n))
                    .Subscribe(Observer.Create<HystrixCommandExecutionStarted>((v) => { }));

            _writeOnlyCommandCompletionSubject
                    .Do((n) => WriteCommandCompletionsToShardedStreams(n))
                    .Subscribe(Observer.Create<HystrixCommandCompletion>((v) => { }));

            _writeOnlyCollapserSubject
                    .Do((n) => WriteCollapserExecutionsToShardedStreams(n))
                    .Subscribe(Observer.Create<HystrixCollapserEvent>((v) => { }));
        }

        public static HystrixThreadEventStream GetInstance()
        {
            return threadLocalStreams.Value;
        }

        public void Shutdown()
        {
            _writeOnlyCommandStartSubject.OnCompleted();
            _writeOnlyCommandCompletionSubject.OnCompleted();
            _writeOnlyCollapserSubject.OnCompleted();
        }

        public void CommandExecutionStarted(IHystrixCommandKey commandKey, IHystrixThreadPoolKey threadPoolKey, ExecutionIsolationStrategy isolationStrategy, int currentConcurrency)
        {
            var @event = new HystrixCommandExecutionStarted(commandKey, threadPoolKey, isolationStrategy, currentConcurrency);
            _writeOnlyCommandStartSubject.OnNext(@event);
        }

        public void ExecutionDone(ExecutionResult executionResult, IHystrixCommandKey commandKey, IHystrixThreadPoolKey threadPoolKey)
        {
            var @event = HystrixCommandCompletion.From(executionResult, commandKey, threadPoolKey);
            _writeOnlyCommandCompletionSubject.OnNext(@event);
        }

        public void CollapserResponseFromCache(IHystrixCollapserKey collapserKey)
        {
            var collapserEvent = HystrixCollapserEvent.From(collapserKey, CollapserEventType.RESPONSE_FROM_CACHE, 1);
            _writeOnlyCollapserSubject.OnNext(collapserEvent);
        }

        public void CollapserBatchExecuted(IHystrixCollapserKey collapserKey, int batchSize)
        {
            var batchExecution = HystrixCollapserEvent.From(collapserKey, CollapserEventType.BATCH_EXECUTED, 1);
            var batchAdditions = HystrixCollapserEvent.From(collapserKey, CollapserEventType.ADDED_TO_BATCH, batchSize);
            _writeOnlyCollapserSubject.OnNext(batchExecution);
            _writeOnlyCollapserSubject.OnNext(batchAdditions);
        }

        public override string ToString()
        {
            return "HystrixThreadEventStream (" + _threadId + " - " + _threadName + ")";
        }

        internal static void Reset()
        {
            foreach (var stream in threadLocalStreams.Values)
            {
                stream.Shutdown();
            }

            threadLocalStreams.Dispose();

            threadLocalStreams = new ThreadLocal<HystrixThreadEventStream>(
            () =>
            {
                return new HystrixThreadEventStream(Thread.CurrentThread.ManagedThreadId);
            }, true);
        }
    }
}
