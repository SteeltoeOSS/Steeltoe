//
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
//

using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric
{
    public class HystrixThreadEventStream
    {
        private readonly long threadId;
        private readonly string threadName;

        private readonly ISubject<HystrixCommandExecutionStarted, HystrixCommandExecutionStarted> writeOnlyCommandStartSubject;
        private readonly ISubject<HystrixCommandCompletion, HystrixCommandCompletion> writeOnlyCommandCompletionSubject;
        private readonly ISubject<HystrixCollapserEvent, HystrixCollapserEvent> writeOnlyCollapserSubject;

        private static readonly ThreadLocal<HystrixThreadEventStream> threadLocalStreams = new ThreadLocal<HystrixThreadEventStream>();


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

        HystrixThreadEventStream(int id)
        {
            this.threadId = id;
            this.threadName = "hystrix-" + threadId;

            writeOnlyCommandStartSubject = Subject.Synchronize<HystrixCommandExecutionStarted, HystrixCommandExecutionStarted>(new Subject<HystrixCommandExecutionStarted>());
            writeOnlyCommandCompletionSubject = Subject.Synchronize<HystrixCommandCompletion, HystrixCommandCompletion>(new Subject<HystrixCommandCompletion>());
            writeOnlyCollapserSubject = Subject.Synchronize<HystrixCollapserEvent, HystrixCollapserEvent>(new Subject<HystrixCollapserEvent>());

            writeOnlyCommandStartSubject
                    // TODO .onBackpressureBuffer()
                    .Do((n) => WriteCommandStartsToShardedStreams(n))
                    .Subscribe(Observer.Create<HystrixCommandExecutionStarted>((v) => { }));

            writeOnlyCommandCompletionSubject
                    // TODO .onBackpressureBuffer()
                    .Do((n) => WriteCommandCompletionsToShardedStreams(n))
                    .Subscribe(Observer.Create<HystrixCommandCompletion>((v) => { }));

            writeOnlyCollapserSubject
                    // TODO .onBackpressureBuffer()
                    .Do((n) => WriteCollapserExecutionsToShardedStreams(n))
                    .Subscribe(Observer.Create<HystrixCollapserEvent>((v) => { }));
        }

        public static HystrixThreadEventStream GetInstance()
        {
            var result = threadLocalStreams.Value;
            if (result == null)
                result = threadLocalStreams.Value = new HystrixThreadEventStream(Thread.CurrentThread.ManagedThreadId);
            return result;
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
    }
}
