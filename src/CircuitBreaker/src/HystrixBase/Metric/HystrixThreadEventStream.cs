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

        private readonly long threadId;
        private readonly string threadName;
        private readonly ISubject<HystrixCommandExecutionStarted, HystrixCommandExecutionStarted> writeOnlyCommandStartSubject;
        private readonly ISubject<HystrixCommandCompletion, HystrixCommandCompletion> writeOnlyCommandCompletionSubject;
        private readonly ISubject<HystrixCollapserEvent, HystrixCollapserEvent> writeOnlyCollapserSubject;

        private static Action<HystrixCommandExecutionStarted> WriteCommandStartsToShardedStreams { get; } = (@event) =>
        {
            HystrixCommandStartStream commandStartStream = HystrixCommandStartStream.GetInstance(@event.CommandKey);
            commandStartStream.Write(@event);

            if (@event.IsExecutedInThread)
            {
                HystrixThreadPoolStartStream threadPoolStartStream = HystrixThreadPoolStartStream.GetInstance(@event.ThreadPoolKey);
                threadPoolStartStream.Write(@event);
            }
        };

        private static Action<HystrixCommandCompletion> WriteCommandCompletionsToShardedStreams { get; } = (commandCompletion) =>
        {
            HystrixCommandCompletionStream commandStream = HystrixCommandCompletionStream.GetInstance(commandCompletion.CommandKey);
            commandStream.Write(commandCompletion);

            if (commandCompletion.IsExecutedInThread || commandCompletion.IsResponseThreadPoolRejected)
            {
                HystrixThreadPoolCompletionStream threadPoolStream = HystrixThreadPoolCompletionStream.GetInstance(commandCompletion.ThreadPoolKey);
                threadPoolStream.Write(commandCompletion);
            }
        };

        private static Action<HystrixCollapserEvent> WriteCollapserExecutionsToShardedStreams { get; } = (collapserEvent) =>
        {
            HystrixCollapserEventStream collapserStream = HystrixCollapserEventStream.GetInstance(collapserEvent.CollapserKey);
            collapserStream.Write(collapserEvent);
        };

        internal HystrixThreadEventStream(int id)
        {
            this.threadId = id;
            this.threadName = "hystrix-" + threadId;

            writeOnlyCommandStartSubject = Subject.Synchronize<HystrixCommandExecutionStarted, HystrixCommandExecutionStarted>(new Subject<HystrixCommandExecutionStarted>());
            writeOnlyCommandCompletionSubject = Subject.Synchronize<HystrixCommandCompletion, HystrixCommandCompletion>(new Subject<HystrixCommandCompletion>());
            writeOnlyCollapserSubject = Subject.Synchronize<HystrixCollapserEvent, HystrixCollapserEvent>(new Subject<HystrixCollapserEvent>());

            writeOnlyCommandStartSubject
                    .Do((n) => WriteCommandStartsToShardedStreams(n))
                    .Subscribe(Observer.Create<HystrixCommandExecutionStarted>((v) => { }));

            writeOnlyCommandCompletionSubject
                    .Do((n) => WriteCommandCompletionsToShardedStreams(n))
                    .Subscribe(Observer.Create<HystrixCommandCompletion>((v) => { }));

            writeOnlyCollapserSubject
                    .Do((n) => WriteCollapserExecutionsToShardedStreams(n))
                    .Subscribe(Observer.Create<HystrixCollapserEvent>((v) => { }));
        }

        public static HystrixThreadEventStream GetInstance()
        {
            return threadLocalStreams.Value;
        }

        public void Shutdown()
        {
            writeOnlyCommandStartSubject.OnCompleted();
            writeOnlyCommandCompletionSubject.OnCompleted();
            writeOnlyCollapserSubject.OnCompleted();
        }

        public void CommandExecutionStarted(IHystrixCommandKey commandKey, IHystrixThreadPoolKey threadPoolKey, ExecutionIsolationStrategy isolationStrategy, int currentConcurrency)
        {
            HystrixCommandExecutionStarted @event = new HystrixCommandExecutionStarted(commandKey, threadPoolKey, isolationStrategy, currentConcurrency);
            writeOnlyCommandStartSubject.OnNext(@event);
        }

        public void ExecutionDone(ExecutionResult executionResult, IHystrixCommandKey commandKey, IHystrixThreadPoolKey threadPoolKey)
        {
            HystrixCommandCompletion @event = HystrixCommandCompletion.From(executionResult, commandKey, threadPoolKey);
            writeOnlyCommandCompletionSubject.OnNext(@event);
        }

        public void CollapserResponseFromCache(IHystrixCollapserKey collapserKey)
        {
            HystrixCollapserEvent collapserEvent = HystrixCollapserEvent.From(collapserKey, CollapserEventType.RESPONSE_FROM_CACHE, 1);
            writeOnlyCollapserSubject.OnNext(collapserEvent);
        }

        public void CollapserBatchExecuted(IHystrixCollapserKey collapserKey, int batchSize)
        {
            HystrixCollapserEvent batchExecution = HystrixCollapserEvent.From(collapserKey, CollapserEventType.BATCH_EXECUTED, 1);
            HystrixCollapserEvent batchAdditions = HystrixCollapserEvent.From(collapserKey, CollapserEventType.ADDED_TO_BATCH, batchSize);
            writeOnlyCollapserSubject.OnNext(batchExecution);
            writeOnlyCollapserSubject.OnNext(batchAdditions);
        }

        public override string ToString()
        {
            return "HystrixThreadEventStream (" + threadId + " - " + threadName + ")";
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
