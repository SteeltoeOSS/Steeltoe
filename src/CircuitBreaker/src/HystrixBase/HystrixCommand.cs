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

using Microsoft.Extensions.Logging;
using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.ExecutionHook;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public class HystrixCommand : HystrixCommand<Unit>, IHystrixExecutable
    {
        protected new readonly Action _run;
        protected new readonly Action _fallback;

        public HystrixCommand(IHystrixCommandGroupKey group, Action run = null, Action fallback = null, ILogger logger = null)
            : this(group, null, null, null, null, null, null, null, null, null, null, null, run, fallback, logger)
        {
        }

        public HystrixCommand(IHystrixCommandGroupKey group, IHystrixThreadPoolKey threadPool, Action run = null,  Action fallback = null, ILogger logger = null)
            : this(group, null, threadPool, null, null, null, null, null, null, null, null, null, run, fallback, logger)
        {
        }

        public HystrixCommand(IHystrixCommandGroupKey group, int executionIsolationThreadTimeoutInMilliseconds, Action run = null, Action fallback = null, ILogger logger = null)
            : this(group, null, null, null, null, new HystrixCommandOptions() { ExecutionTimeoutInMilliseconds = executionIsolationThreadTimeoutInMilliseconds }, null, null, null, null, null, null, run, fallback, logger)
        {
        }

        public HystrixCommand(IHystrixCommandGroupKey group, IHystrixThreadPoolKey threadPool, int executionIsolationThreadTimeoutInMilliseconds, Action run = null, Action fallback = null, ILogger logger = null)
            : this(group, null, threadPool, null, null, new HystrixCommandOptions() { ExecutionTimeoutInMilliseconds = executionIsolationThreadTimeoutInMilliseconds }, null, null, null, null, null, null, run, fallback, logger)
        {
        }

        public HystrixCommand(IHystrixCommandOptions commandOptions, Action run = null, Action fallback = null, ILogger logger = null)
            : this(commandOptions.GroupKey, commandOptions.CommandKey, commandOptions.ThreadPoolKey, null, null, commandOptions, commandOptions.ThreadPoolOptions, null, null, null, null, null, run, fallback, logger)
        {
        }

        public HystrixCommand(
            IHystrixCommandGroupKey group,
            IHystrixCommandKey key,
            IHystrixThreadPoolKey threadPoolKey,
            IHystrixCircuitBreaker circuitBreaker,
            IHystrixThreadPool threadPool,
            IHystrixCommandOptions commandOptionsDefaults,
            IHystrixThreadPoolOptions threadPoolOptionsDefaults,
            HystrixCommandMetrics metrics,
            SemaphoreSlim fallbackSemaphore,
            SemaphoreSlim executionSemaphore,
            HystrixOptionsStrategy optionsStrategy,
            HystrixCommandExecutionHook executionHook,
            Action run,
            Action fallback,
            ILogger logger = null)
            : base(group, key, threadPoolKey, circuitBreaker, threadPool, commandOptionsDefaults, threadPoolOptionsDefaults, metrics, fallbackSemaphore, executionSemaphore, optionsStrategy, executionHook, null, null, logger)
        {
            if (run == null)
            {
                _run = () => Run();
            }
            else
            {
                _run = run;
            }

            if (fallback == null)
            {
                _fallback = () => RunFallback();
            }
            else
            {
                _fallback = fallback;
            }
        }

        public new void Execute()
        {
            base.Execute();
        }

        public new Task ExecuteAsync(CancellationToken token)
        {
            return base.ExecuteAsync(token);
        }

        public new Task ExecuteAsync()
        {
            return base.ExecuteAsync();
        }

        protected new virtual void Run() => RunAsync().GetAwaiter().GetResult();

        protected new virtual void RunFallback() => RunFallbackAsync().GetAwaiter().GetResult();

        protected override Unit DoRun()
        {
            _run();
            return Unit.Default;
        }

        protected override Unit DoFallback()
        {
            RunFallback();
            return Unit.Default;
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class HystrixCommand<TResult> : AbstractCommand<TResult>, IHystrixExecutable<TResult>
#pragma warning restore SA1402 // File may only contain a single class
    {
        protected readonly Func<TResult> _run;
        protected readonly Func<TResult> _fallback;

        public HystrixCommand(IHystrixCommandGroupKey group, Func<TResult> run = null, Func<TResult> fallback = null, ILogger logger = null)
            : this(group, null, null, null, null, null, null, null, null, null, null, null, run, fallback, logger)
        {
        }

        public HystrixCommand(IHystrixCommandGroupKey group, IHystrixThreadPoolKey threadPool, Func<TResult> run = null, Func<TResult> fallback = null, ILogger logger = null)
            : this(group, null, threadPool, null, null, null, null, null, null, null, null, null, run, fallback, logger)
        {
        }

        public HystrixCommand(IHystrixCommandGroupKey group, int executionIsolationThreadTimeoutInMilliseconds, Func<TResult> run = null, Func<TResult> fallback = null, ILogger logger = null)
            : this(group, null, null, null, null, new HystrixCommandOptions() { ExecutionTimeoutInMilliseconds = executionIsolationThreadTimeoutInMilliseconds }, null, null, null, null, null, null, run, fallback, logger)
        {
        }

        public HystrixCommand(IHystrixCommandGroupKey group, IHystrixThreadPoolKey threadPool, int executionIsolationThreadTimeoutInMilliseconds, Func<TResult> run = null, Func<TResult> fallback = null, ILogger logger = null)
            : this(group, null, threadPool, null, null, new HystrixCommandOptions() { ExecutionTimeoutInMilliseconds = executionIsolationThreadTimeoutInMilliseconds }, null, null, null, null, null, null, run, fallback, logger)
        {
        }

        public HystrixCommand(IHystrixCommandOptions commandOptions, Func<TResult> run = null, Func<TResult> fallback = null, ILogger logger = null)
            : this(commandOptions.GroupKey, commandOptions.CommandKey, commandOptions.ThreadPoolKey, null, null, commandOptions, commandOptions.ThreadPoolOptions, null, null, null, null, null, run, fallback, logger)
        {
        }

        public HystrixCommand(
            IHystrixCommandGroupKey group,
            IHystrixCommandKey key,
            IHystrixThreadPoolKey threadPoolKey,
            IHystrixCircuitBreaker circuitBreaker,
            IHystrixThreadPool threadPool,
            IHystrixCommandOptions commandOptionsDefaults,
            IHystrixThreadPoolOptions threadPoolOptionsDefaults,
            HystrixCommandMetrics metrics,
            SemaphoreSlim fallbackSemaphore,
            SemaphoreSlim executionSemaphore,
            HystrixOptionsStrategy optionsStrategy,
            HystrixCommandExecutionHook executionHook,
            Func<TResult> run,
            Func<TResult> fallback,
            ILogger logger = null)
            : base(group, key, threadPoolKey, circuitBreaker, threadPool, commandOptionsDefaults, threadPoolOptionsDefaults, metrics, fallbackSemaphore, executionSemaphore, optionsStrategy, executionHook, logger)
        {
            if (run == null)
            {
                _run = () => Run();
            }
            else
            {
                _run = run;
            }

            if (fallback == null)
            {
                _fallback = () => RunFallback();
            }
            else
            {
                _fallback = fallback;
            }
        }

        public TResult Execute()
        {
            try
            {
                return ExecuteAsync().GetAwaiter().GetResult();
            }
            catch (Exception e) when (!(e is HystrixRuntimeException))
            {
                throw DecomposeException(e);
            }
        }

        public Task<TResult> ExecuteAsync(CancellationToken token)
        {
            _usersToken = token;

            Task<TResult> toStart = ToTask();
            if (!toStart.IsCompleted)
            {
                if (_execThreadTask != null)
                {
                    StartCommand();
                }
                else
                {
                    tcs.TrySetException(new HystrixRuntimeException(FailureType.BAD_REQUEST_EXCEPTION, GetType(), "Thread task missing"));
                    tcs.Commit();
                }
            }

            return toStart;
        }

        public Task<TResult> ExecuteAsync()
        {
            return ExecuteAsync(CancellationToken.None);
        }

        public IObservable<TResult> Observe()
        {
            ReplaySubject<TResult> subject = new ReplaySubject<TResult>();
            IObservable<TResult> observable = ToObservable();
            var disposable = observable.Subscribe(subject);
            return subject.Finally(() => disposable.Dispose());
        }

        public IObservable<TResult> Observe(CancellationToken token)
        {
            ReplaySubject<TResult> subject = new ReplaySubject<TResult>();
            IObservable<TResult> observable = ToObservable();
            observable.Subscribe(subject, token);
            return observable;
        }

        public IObservable<TResult> ToObservable()
        {
            IObservable<TResult> observable = Observable.FromAsync<TResult>((ct) =>
            {
                this._usersToken = ct;
                Task<TResult> toStart = ToTask();
                if (!toStart.IsCompleted)
                {
                    if (this._execThreadTask != null)
                    {
                        StartCommand();
                    }
                    else
                    {
                        tcs.TrySetException(new HystrixRuntimeException(FailureType.BAD_REQUEST_EXCEPTION, GetType(), "Thread task missing"));
                        tcs.Commit();
                    }
                }
                return toStart;
            });
            return observable;
        }

        internal Task<TResult> ToTask()
        {
            Setup();

            if (PutInCacheIfAbsent(tcs.Task, out Task<TResult> fromCache))
            {
                var task = fromCache;
                return task;
            }

            ApplyHystrixSemantics();

            return tcs.Task;
        }

        protected virtual TResult Run()
        {
            return RunAsync().GetAwaiter().GetResult();
        }

        protected virtual TResult RunFallback()
        {
            return RunFallbackAsync().GetAwaiter().GetResult();
        }

        protected virtual async Task<TResult> RunAsync()
        {
            return await Task.FromResult(default(TResult)).ConfigureAwait(false);
        }

        protected virtual async Task<TResult> RunFallbackAsync()
        {
            return await Task.FromException<TResult>(new InvalidOperationException("No fallback available.")).ConfigureAwait(false);
        }

        protected override TResult DoRun()
        {
            return _run();
        }

        protected override TResult DoFallback()
        {
            return _fallback();
        }
    }
}
