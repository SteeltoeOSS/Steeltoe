// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.ExecutionHook;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;

namespace Steeltoe.CircuitBreaker.Hystrix;

public class HystrixCommand : HystrixCommand<Unit>, IHystrixExecutable
{
    protected new readonly Action RunCallback;
    protected new readonly Action FallbackCallback;

    public HystrixCommand(IHystrixCommandGroupKey group, Action run = null, Action fallback = null, ILogger logger = null)
        : this(group, null, null, null, null, null, null, null, null, null, null, null, run, fallback, logger)
    {
    }

    public HystrixCommand(IHystrixCommandGroupKey group, IHystrixThreadPoolKey threadPool, Action run = null, Action fallback = null, ILogger logger = null)
        : this(group, null, threadPool, null, null, null, null, null, null, null, null, null, run, fallback, logger)
    {
    }

    public HystrixCommand(IHystrixCommandGroupKey group, int executionIsolationThreadTimeoutInMilliseconds, Action run = null, Action fallback = null,
        ILogger logger = null)
        : this(group, null, null, null, null, new HystrixCommandOptions
        {
            ExecutionTimeoutInMilliseconds = executionIsolationThreadTimeoutInMilliseconds
        }, null, null, null, null, null, null, run, fallback, logger)
    {
    }

    public HystrixCommand(IHystrixCommandGroupKey group, IHystrixThreadPoolKey threadPool, int executionIsolationThreadTimeoutInMilliseconds, Action run = null,
        Action fallback = null, ILogger logger = null)
        : this(group, null, threadPool, null, null, new HystrixCommandOptions
        {
            ExecutionTimeoutInMilliseconds = executionIsolationThreadTimeoutInMilliseconds
        }, null, null, null, null, null, null, run, fallback, logger)
    {
    }

    public HystrixCommand(IHystrixCommandOptions commandOptions, Action run = null, Action fallback = null, ILogger logger = null)
        : this(commandOptions.GroupKey, commandOptions.CommandKey, commandOptions.ThreadPoolKey, null, null, commandOptions, commandOptions.ThreadPoolOptions,
            null, null, null, null, null, run, fallback, logger)
    {
    }

    public HystrixCommand(IHystrixCommandGroupKey group, IHystrixCommandKey key, IHystrixThreadPoolKey threadPoolKey, ICircuitBreaker circuitBreaker,
        IHystrixThreadPool threadPool, IHystrixCommandOptions commandOptionsDefaults, IHystrixThreadPoolOptions threadPoolOptionsDefaults,
        HystrixCommandMetrics metrics, SemaphoreSlim fallbackSemaphore, SemaphoreSlim executionSemaphore, HystrixOptionsStrategy optionsStrategy,
        HystrixCommandExecutionHook executionHook, Action run, Action fallback, ILogger logger = null)
        : base(group, key, threadPoolKey, circuitBreaker, threadPool, commandOptionsDefaults, threadPoolOptionsDefaults, metrics, fallbackSemaphore,
            executionSemaphore, optionsStrategy, executionHook, null, null, logger)
    {
        RunCallback = run ?? Run;
        FallbackCallback = fallback ?? RunFallback;
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

    protected new virtual void Run()
    {
        RunAsync().GetAwaiter().GetResult();
    }

    protected new virtual void RunFallback()
    {
        RunFallbackAsync().GetAwaiter().GetResult();
    }

    protected override Unit DoRun()
    {
        RunCallback();
        return Unit.Default;
    }

    protected override Unit DoFallback()
    {
        RunFallback();
        return Unit.Default;
    }
}

public class HystrixCommand<TResult> : AbstractCommand<TResult>, IHystrixExecutable<TResult>
{
    protected readonly Func<TResult> RunCallback;
    protected readonly Func<TResult> FallbackCallback;

    public HystrixCommand(IHystrixCommandGroupKey group, Func<TResult> run = null, Func<TResult> fallback = null, ILogger logger = null)
        : this(group, null, null, null, null, null, null, null, null, null, null, null, run, fallback, logger)
    {
    }

    public HystrixCommand(IHystrixCommandGroupKey group, IHystrixThreadPoolKey threadPool, Func<TResult> run = null, Func<TResult> fallback = null,
        ILogger logger = null)
        : this(group, null, threadPool, null, null, null, null, null, null, null, null, null, run, fallback, logger)
    {
    }

    public HystrixCommand(IHystrixCommandGroupKey group, int executionIsolationThreadTimeoutInMilliseconds, Func<TResult> run = null,
        Func<TResult> fallback = null, ILogger logger = null)
        : this(group, null, null, null, null, new HystrixCommandOptions
        {
            ExecutionTimeoutInMilliseconds = executionIsolationThreadTimeoutInMilliseconds
        }, null, null, null, null, null, null, run, fallback, logger)
    {
    }

    public HystrixCommand(IHystrixCommandGroupKey group, IHystrixThreadPoolKey threadPool, int executionIsolationThreadTimeoutInMilliseconds,
        Func<TResult> run = null, Func<TResult> fallback = null, ILogger logger = null)
        : this(group, null, threadPool, null, null, new HystrixCommandOptions
        {
            ExecutionTimeoutInMilliseconds = executionIsolationThreadTimeoutInMilliseconds
        }, null, null, null, null, null, null, run, fallback, logger)
    {
    }

    public HystrixCommand(IHystrixCommandOptions commandOptions, Func<TResult> run = null, Func<TResult> fallback = null, ILogger logger = null)
        : this(commandOptions.GroupKey, commandOptions.CommandKey, commandOptions.ThreadPoolKey, null, null, commandOptions, commandOptions.ThreadPoolOptions,
            null, null, null, null, null, run, fallback, logger)
    {
    }

    public HystrixCommand(IHystrixCommandGroupKey group, IHystrixCommandKey key, IHystrixThreadPoolKey threadPoolKey, ICircuitBreaker circuitBreaker,
        IHystrixThreadPool threadPool, IHystrixCommandOptions commandOptionsDefaults, IHystrixThreadPoolOptions threadPoolOptionsDefaults,
        HystrixCommandMetrics metrics, SemaphoreSlim fallbackSemaphore, SemaphoreSlim executionSemaphore, HystrixOptionsStrategy optionsStrategy,
        HystrixCommandExecutionHook executionHook, Func<TResult> run, Func<TResult> fallback, ILogger logger = null)
        : base(group, key, threadPoolKey, circuitBreaker, threadPool, commandOptionsDefaults, threadPoolOptionsDefaults, metrics, fallbackSemaphore,
            executionSemaphore, optionsStrategy, executionHook, logger)
    {
        RunCallback = run ?? Run;
        FallbackCallback = fallback ?? RunFallback;
    }

    public TResult Execute()
    {
        try
        {
            return ExecuteAsync().GetAwaiter().GetResult();
        }
        catch (Exception e) when (e is not HystrixRuntimeException)
        {
            throw DecomposeException(e);
        }
    }

    public Task<TResult> ExecuteAsync(CancellationToken token)
    {
        UsersToken = token;

        Task<TResult> toStart = ToTaskAsync();

        if (!toStart.IsCompleted)
        {
            if (ExecThreadTask != null)
            {
                StartCommand();
            }
            else
            {
                tcs.TrySetException(new HystrixRuntimeException(FailureType.BadRequestException, GetType(), "Thread task missing"));
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
        var subject = new ReplaySubject<TResult>();
        IObservable<TResult> observable = ToObservable();
        IDisposable disposable = observable.Subscribe(subject);
        return subject.Finally(() => disposable.Dispose());
    }

    public IObservable<TResult> Observe(CancellationToken token)
    {
        var subject = new ReplaySubject<TResult>();
        IObservable<TResult> observable = ToObservable();
        observable.Subscribe(subject, token);
        return observable;
    }

    public IObservable<TResult> ToObservable()
    {
        IObservable<TResult> observable = Observable.FromAsync(ct =>
        {
            UsersToken = ct;
            Task<TResult> toStart = ToTaskAsync();

            if (!toStart.IsCompleted)
            {
                if (ExecThreadTask != null)
                {
                    StartCommand();
                }
                else
                {
                    tcs.TrySetException(new HystrixRuntimeException(FailureType.BadRequestException, GetType(), "Thread task missing"));
                    tcs.Commit();
                }
            }

            return toStart;
        });

        return observable;
    }

    internal Task<TResult> ToTaskAsync()
    {
        Setup();

        if (PutInCacheIfAbsent(tcs.Task, out Task<TResult> fromCache))
        {
            Task<TResult> task = fromCache;
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
        return await Task.FromResult(default(TResult));
    }

    protected virtual async Task<TResult> RunFallbackAsync()
    {
        return await Task.FromException<TResult>(new InvalidOperationException("No fallback available."));
    }

    protected override TResult DoRun()
    {
        return RunCallback();
    }

    protected override TResult DoFallback()
    {
        return FallbackCallback();
    }
}
