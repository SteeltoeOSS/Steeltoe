// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.CircuitBreaker.Hystrix.CircuitBreaker;
using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Strategy;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.EventNotifier;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.ExecutionHook;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Metrics;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;
using Steeltoe.CircuitBreaker.Hystrix.ThreadPool;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.Common;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix;

public abstract class AbstractCommand<TResult> : AbstractCommandBase, IHystrixInvokableInfo, IHystrixInvokable
{
    protected enum TimedOutStatus
    {
        NotExecuted,
        Completed,
        TimedOut
    }

    protected enum CommandState
    {
        NotStarted,
        ObservableChainCreated,
        UserCodeExecuted,
        Unsubscribed,
        Terminal
    }

    protected enum ThreadState
    {
        NotUsingThread,
        Started,
        Unsubscribed,
        Terminal
    }

    protected class AtomicCommandState : AtomicInteger
    {
        public AtomicCommandState(CommandState state)
            : base((int)state)
        {
        }

        public new CommandState Value
        {
            get => (CommandState)innerValue;

            set => base.innerValue = (int)value;
        }

        public bool CompareAndSet(CommandState expected, CommandState update)
        {
            return CompareAndSet((int)expected, (int)update);
        }
    }

    protected class AtomicThreadState : AtomicInteger
    {
        public AtomicThreadState(ThreadState state)
            : base((int)state)
        {
        }

        public new ThreadState Value
        {
            get => (ThreadState)innerValue;

            set => base.innerValue = (int)value;
        }

        public bool CompareAndSet(ThreadState expected, ThreadState update)
        {
            return CompareAndSet((int)expected, (int)update);
        }
    }

    protected class AtomicTimedOutStatus : AtomicInteger
    {
        public AtomicTimedOutStatus(TimedOutStatus state)
            : base((int)state)
        {
        }

        public new TimedOutStatus Value
        {
            get => (TimedOutStatus)innerValue;

            set => base.innerValue = (int)value;
        }

        public bool CompareAndSet(TimedOutStatus expected, TimedOutStatus update)
        {
            return CompareAndSet((int)expected, (int)update);
        }
    }

    protected class HystrixCompletionSource
    {
        private bool? _canceled;
        private bool _resultSet;

        public HystrixCompletionSource(AbstractCommand<TResult> cmd)
        {
            Source = new TaskCompletionSource<TResult>(cmd);
            _resultSet = false;
        }

        public TaskCompletionSource<TResult> Source { get; }

        public bool IsCanceled => _canceled ?? false;

        public bool IsCompleted => IsFaulted || IsCanceled || _resultSet;

        public bool IsFaulted => Exception != null;

        public Exception Exception { get; private set; }

        public Task<TResult> Task => Source.Task;

        public TResult Result { get; private set; }

        internal void TrySetException(Exception exception)
        {
            if (!IsCompleted)
            {
                Exception = exception;
            }
        }

        internal void TrySetCanceled()
        {
            if (!IsCompleted)
            {
                _canceled = true;
            }
        }

        internal void TrySetResult(TResult result)
        {
            if (!IsCompleted)
            {
                _resultSet = true;
                Result = result;
            }
        }

        internal void Commit()
        {
            if (!IsCompleted)
            {
                throw new InvalidOperationException("HystrixCompletionSource not completed!");
            }

            if (IsCanceled)
            {
                Source.SetCanceled();
            }
            else if (IsFaulted)
            {
                Source.SetException(Exception);
            }
            else
            {
                Source.SetResult(Result);
            }
        }
    }

    protected internal readonly HystrixRequestLog CurrentRequestLog;
    protected internal readonly HystrixRequestCache RequestCache;
    protected internal readonly HystrixCommandExecutionHook ExecutionHook;
    protected internal readonly HystrixCommandMetrics InnerMetrics;
    protected internal readonly HystrixEventNotifier EventNotifier;
    protected internal readonly ICircuitBreaker InnerCircuitBreaker;
    protected internal readonly IHystrixThreadPool ThreadPool;
    protected internal readonly SemaphoreSlim FallbackSemaphoreOverride;
    protected internal readonly SemaphoreSlim ExecutionSemaphoreOverride;
    protected internal readonly HystrixConcurrencyStrategy ConcurrencyStrategy;
    protected internal long CommandStartTimestamp = -1L;
    protected internal long ThreadStartTimestamp = -1L;
    protected internal volatile bool InnerIsResponseFromCache;
    protected internal Task ExecThreadTask;
    protected internal CancellationTokenSource TimeoutTcs;
    protected internal CancellationToken Token;
    protected internal CancellationToken UsersToken;
    protected internal volatile ExecutionResult ExecutionResult = Empty; // state on shared execution
    protected internal volatile ExecutionResult ExecutionResultAtTimeOfCancellation;

    protected readonly AtomicCommandState InnerCommandState = new (CommandState.NotStarted);
    protected readonly AtomicThreadState InnerThreadState = new (ThreadState.NotUsingThread);
    protected readonly AtomicTimedOutStatus IsCommandTimedOut = new (TimedOutStatus.NotExecuted);
    protected readonly IHystrixCommandGroupKey InnerCommandGroup;

    protected HystrixCompletionSource tcs;
    private readonly ILogger _logger;

    protected AbstractCommand(
        IHystrixCommandGroupKey group,
        IHystrixCommandKey key,
        IHystrixThreadPoolKey threadPoolKey,
        ICircuitBreaker circuitBreaker,
        IHystrixThreadPool threadPool,
        IHystrixCommandOptions commandOptionsDefaults,
        IHystrixThreadPoolOptions threadPoolOptionsDefaults,
        HystrixCommandMetrics metrics,
        SemaphoreSlim fallbackSemaphore,
        SemaphoreSlim executionSemaphore,
        HystrixOptionsStrategy optionsStrategy,
        HystrixCommandExecutionHook executionHook,
        ILogger logger = null)
    {
        _logger = logger;
        InnerCommandGroup = InitGroupKey(group);
        InnerCommandKey = InitCommandKey(key, GetType());
        InnerOptions = InitCommandOptions(InnerCommandKey, optionsStrategy, commandOptionsDefaults);
        this.InnerThreadPoolKey = InitThreadPoolKey(threadPoolKey, InnerCommandGroup, InnerOptions.ExecutionIsolationThreadPoolKeyOverride);
        this.InnerMetrics = InitMetrics(metrics, InnerCommandGroup, this.InnerThreadPoolKey, InnerCommandKey, InnerOptions);
        this.InnerCircuitBreaker = InitCircuitBreaker(InnerOptions.CircuitBreakerEnabled, circuitBreaker, InnerCommandGroup, InnerCommandKey, InnerOptions, this.InnerMetrics);

        this.ThreadPool = InitThreadPool(threadPool, this.InnerThreadPoolKey, threadPoolOptionsDefaults);

        // Strategies from plugins
        EventNotifier = HystrixPlugins.EventNotifier;
        ConcurrencyStrategy = HystrixPlugins.ConcurrencyStrategy;
        HystrixMetricsPublisherFactory.CreateOrRetrievePublisherForCommand(InnerCommandKey, InnerCommandGroup, this.InnerMetrics, this.InnerCircuitBreaker, InnerOptions);

        this.ExecutionHook = InitExecutionHook(executionHook);

        RequestCache = HystrixRequestCache.GetInstance(InnerCommandKey);
        CurrentRequestLog = InitRequestLog(InnerOptions.RequestLogEnabled);

        /* fallback semaphore override if applicable */
        FallbackSemaphoreOverride = fallbackSemaphore;

        /* execution semaphore override if applicable */
        ExecutionSemaphoreOverride = executionSemaphore;
    }

    internal void MarkAsCollapsedCommand(IHystrixCollapserKey collapserKey, int sizeOfBatch)
    {
        MarkCollapsedCommand(collapserKey, sizeOfBatch);
    }

    protected internal SemaphoreSlim GetExecutionSemaphore()
    {
        if (InnerOptions.ExecutionIsolationStrategy == ExecutionIsolationStrategy.Semaphore)
        {
            if (ExecutionSemaphoreOverride == null)
            {
                return ExecutionSemaphorePerCircuit.GetOrAddEx(InnerCommandKey.Name, _ => new SemaphoreSlim(InnerOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests));
            }
            else
            {
                return ExecutionSemaphoreOverride;
            }
        }
        else
        {
            // return NoOp implementation since we're not using SEMAPHORE isolation
            return null;
        }
    }

    protected internal SemaphoreSlim GetFallbackSemaphore()
    {
        if (FallbackSemaphoreOverride == null)
        {
            return FallbackSemaphorePerCircuit.GetOrAddEx(InnerCommandKey.Name, _ => new SemaphoreSlim(InnerOptions.FallbackIsolationSemaphoreMaxConcurrentRequests));
        }
        else
        {
            return FallbackSemaphoreOverride;
        }
    }

    protected static IHystrixCommandGroupKey InitGroupKey(IHystrixCommandGroupKey fromConstructor)
    {
        if (fromConstructor == null)
        {
            throw new ArgumentNullException("HystrixCommandGroupKey can not be NULL");
        }
        else
        {
            return fromConstructor;
        }
    }

    protected static IHystrixCommandKey InitCommandKey(IHystrixCommandKey fromConstructor, Type clazz)
    {
        if (fromConstructor == null || string.IsNullOrWhiteSpace(fromConstructor.Name))
        {
            var keyName = clazz.Name;
            return HystrixCommandKeyDefault.AsKey(keyName);
        }
        else
        {
            return fromConstructor;
        }
    }

    protected static IHystrixCommandOptions InitCommandOptions(
        IHystrixCommandKey commandKey,
        HystrixOptionsStrategy optionsStrategy,
        IHystrixCommandOptions commandOptionsDefault)
    {
        if (optionsStrategy == null)
        {
            return HystrixOptionsFactory.GetCommandOptions(commandKey, commandOptionsDefault);
        }
        else
        {
            // used for unit testing
            return optionsStrategy.GetCommandOptions(commandKey, commandOptionsDefault);
        }
    }

    protected static IHystrixThreadPoolKey InitThreadPoolKey(
        IHystrixThreadPoolKey threadPoolKey,
        IHystrixCommandGroupKey groupKey,
        string threadPoolKeyOverride)
    {
        if (threadPoolKeyOverride == null)
        {
            // we don't have a property overriding the value so use either HystrixThreadPoolKey or HystrixCommandGroup
            if (threadPoolKey == null)
            {
                /* use HystrixCommandGroup if HystrixThreadPoolKey is null */
                return HystrixThreadPoolKeyDefault.AsKey(groupKey.Name);
            }
            else
            {
                return threadPoolKey;
            }
        }
        else
        {
            // we have a property defining the thread-pool so use it instead
            return HystrixThreadPoolKeyDefault.AsKey(threadPoolKeyOverride);
        }
    }

    protected static HystrixCommandMetrics InitMetrics(
        HystrixCommandMetrics fromConstructor,
        IHystrixCommandGroupKey groupKey,
        IHystrixThreadPoolKey threadPoolKey,
        IHystrixCommandKey commandKey,
        IHystrixCommandOptions properties)
    {
        if (fromConstructor == null)
        {
            return HystrixCommandMetrics.GetInstance(commandKey, groupKey, threadPoolKey, properties);
        }
        else
        {
            return fromConstructor;
        }
    }

    protected static ICircuitBreaker InitCircuitBreaker(
        bool enabled,
        ICircuitBreaker fromConstructor,
        IHystrixCommandGroupKey groupKey,
        IHystrixCommandKey commandKey,
        IHystrixCommandOptions properties,
        HystrixCommandMetrics metrics)
    {
        if (enabled)
        {
            if (fromConstructor == null)
            {
                // get the default implementation of HystrixCircuitBreaker
                return HystrixCircuitBreakerFactory.GetInstance(commandKey, groupKey, properties, metrics);
            }
            else
            {
                return fromConstructor;
            }
        }
        else
        {
            return new NoOpCircuitBreaker();
        }
    }

    protected static HystrixCommandExecutionHook InitExecutionHook(HystrixCommandExecutionHook fromConstructor)
    {
        if (fromConstructor == null)
        {
            return HystrixPlugins.CommandExecutionHook;
        }

        return fromConstructor;
    }

    protected static IHystrixThreadPool InitThreadPool(
        IHystrixThreadPool fromConstructor,
        IHystrixThreadPoolKey threadPoolKey,
        IHystrixThreadPoolOptions threadPoolPropertiesDefaults)
    {
        if (fromConstructor == null)
        {
            // get the default implementation of HystrixThreadPool
            return HystrixThreadPoolFactory.GetInstance(threadPoolKey, threadPoolPropertiesDefaults);
        }
        else
        {
            return fromConstructor;
        }
    }

    protected static HystrixRequestLog InitRequestLog(bool enabled)
    {
        if (enabled)
        {
            /* store reference to request log regardless of which thread later hits it */
            return HystrixRequestLog.CurrentRequestLog;
        }
        else
        {
            return null;
        }
    }

    protected void Setup()
    {
        TimeoutTcs = CancellationTokenSource.CreateLinkedTokenSource(UsersToken);
        Token = TimeoutTcs.Token;
        tcs = new HystrixCompletionSource(this);

        /* this is a stateful object so can only be used once */
        if (!InnerCommandState.CompareAndSet(CommandState.NotStarted, CommandState.ObservableChainCreated))
        {
            var ex = new InvalidOperationException("This instance can only be executed once. Please instantiate a new instance.");
            throw new HystrixRuntimeException(
                FailureType.BadRequestException,
                GetType(),
                $"{LogMessagePrefix} command executed multiple times - this is not permitted.",
                ex,
                null);
        }

        CommandStartTimestamp = Time.CurrentTimeMillis;

        if (CommandOptions.RequestLogEnabled && CurrentRequestLog != null)
        {
            // log this command execution regardless of what happened
            CurrentRequestLog.AddExecutedCommand(this);
        }
    }

    protected bool PutInCacheIfAbsent(Task<TResult> hystrixTask, out Task<TResult> fromCache)
    {
        fromCache = null;
        if (IsRequestCachingEnabled && CacheKey != null)
        {
            // wrap it for caching
            fromCache = RequestCache.PutIfAbsent(CacheKey, hystrixTask);
            if (fromCache != null)
            {
                // another thread beat us so we'll use the cached value instead
                InnerIsResponseFromCache = true;
                HandleRequestCacheHitAndEmitValues(fromCache, this);
                return true;
            }
        }

        return false;
    }

    protected void ApplyHystrixSemantics()
    {
        if (InnerCommandState.Value.Equals(CommandState.Unsubscribed))
        {
            return;
        }

        try
        {
            // mark that we're starting execution on the ExecutionHook
            // if this hook throws an exception, then a fast-fail occurs with no fallback.  No state is left inconsistent
            ExecutionHook.OnStart(this);

            /* determine if we're allowed to execute */
            if (InnerCircuitBreaker.AllowRequest)
            {
                var executionSemaphore = GetExecutionSemaphore();

                if (executionSemaphore.TryAcquire())
                {
                    try
                    {
                        /* used to track userThreadExecutionTime */
                        ExecutionResult = ExecutionResult.SetInvocationStartTime(Time.CurrentTimeMillis);
                        ExecuteCommandWithSpecifiedIsolation();
                        if (tcs.IsFaulted)
                        {
                            EventNotifier.MarkEvent(HystrixEventType.ExceptionThrown, CommandKey);
                        }
                    }
                    finally
                    {
                        executionSemaphore?.Release();
                    }
                }
                else
                {
                    HandleSemaphoreRejectionViaFallback();
                }
            }
            else
            {
                HandleShortCircuitViaFallback();

                if (tcs.IsFaulted)
                {
                    EventNotifier.MarkEvent(HystrixEventType.ExceptionThrown, CommandKey);
                }
            }
        }
        finally
        {
            if (IsCommandTimedOut.Value != TimedOutStatus.TimedOut)
            {
                UnsubscribeCommandCleanup();
                FireOnCompletedHook();
                TerminateCommandCleanup();
            }
        }
    }

    protected void StartCommand()
    {
        try
        {
            ExecThreadTask.Start(ThreadPool.GetTaskScheduler());
        }
        catch (Exception e)
        {
            HandleFallback(e);
            if (tcs.IsFaulted)
            {
                EventNotifier.MarkEvent(HystrixEventType.ExceptionThrown, CommandKey);
            }

            UnsubscribeCommandCleanup();
            FireOnCompletedHook();
            TerminateCommandCleanup();
        }
    }

    protected Exception DecomposeException(Exception e)
    {
        if (e is HystrixBadRequestException exception)
        {
            return exception;
        }

        if (e.InnerException is HystrixBadRequestException exception1)
        {
            return exception1;
        }

        if (e is HystrixRuntimeException exception2)
        {
            return exception2;
        }

        // if we have an exception we know about we'll throw it directly without the wrapper exception
        if (e.InnerException is HystrixRuntimeException exception3)
        {
            return exception3;
        }

        // we don't know what kind of exception this is so create a generic message and throw a new HystrixRuntimeException
        var message = $"{LogMessagePrefix} failed while executing. {{0}}";
        _logger?.LogDebug(message, e); // debug only since we're throwing the exception and someone higher will do something with it
        return new HystrixRuntimeException(FailureType.CommandException, GetType(), message, e, null);
    }

    protected abstract TResult DoRun();

    protected abstract TResult DoFallback();

    protected virtual void HandleCleanUpAfterResponseFromCache(bool commandExecutionStarted)
    {
        var latency = Time.CurrentTimeMillis - CommandStartTimestamp;
        ExecutionResult = ExecutionResult.AddEvent(-1, HystrixEventType.ResponseFromCache)
            .MarkUserThreadCompletion(latency)
            .SetNotExecutedInThread();
        var cacheOnlyForMetrics = ExecutionResult.From(HystrixEventType.ResponseFromCache)
            .MarkUserThreadCompletion(latency);
        InnerMetrics.MarkCommandDone(cacheOnlyForMetrics, InnerCommandKey, InnerThreadPoolKey, commandExecutionStarted);
        EventNotifier.MarkEvent(HystrixEventType.ResponseFromCache, InnerCommandKey);
    }

    protected virtual void HandleCommandEnd(bool commandExecutionStarted)
    {
        var userThreadLatency = Time.CurrentTimeMillis - CommandStartTimestamp;
        ExecutionResult = ExecutionResult.MarkUserThreadCompletion((int)userThreadLatency);
        InnerMetrics.MarkCommandDone(ExecutionResultAtTimeOfCancellation ?? ExecutionResult, InnerCommandKey, InnerThreadPoolKey, commandExecutionStarted);
    }

    protected virtual void HandleThreadEnd()
    {
        HystrixCounters.DecrementGlobalConcurrentThreads();
        ThreadPool.MarkThreadCompletion();
        try
        {
            ExecutionHook.OnThreadComplete(this);
        }
        catch (Exception hookEx)
        {
            _logger?.LogWarning("Error calling HystrixCommandExecutionHook.onThreadComplete: {0}", hookEx);
        }
    }

    private void HandleFallbackOrThrowException(HystrixEventType eventType, FailureType failureType, string message, Exception originalException)
    {
        var latency = Time.CurrentTimeMillis - ExecutionResult.StartTimestamp;

        // record the executionResult
        // do this before executing fallback so it can be queried from within getFallback (see See https://github.com/Netflix/Hystrix/pull/144)
        ExecutionResult = ExecutionResult.AddEvent((int)latency, eventType);

        if (IsUnrecoverableError(originalException))
        {
            var e = originalException;
            _logger?.LogError("Unrecoverable Error for HystrixCommand so will throw HystrixRuntimeException and not apply fallback: {0} ", e);

            /* executionHook for all errors */
            e = WrapWithOnErrorHook(failureType, e);
            tcs.TrySetException(new HystrixRuntimeException(failureType, GetType(), $"{LogMessagePrefix} {message} and encountered unrecoverable error.", e, null));
        }
        else
        {
            if (IsRecoverableError(originalException))
            {
                _logger?.LogWarning("Recovered from Error by serving Hystrix fallback: {0}", originalException);
            }

            if (InnerOptions.FallbackEnabled)
            {
                /* fallback behavior is permitted so attempt */

                var fallbackSemaphore = GetFallbackSemaphore();

                TResult fallbackExecutionResult;

                // acquire a permit
                if (fallbackSemaphore.TryAcquire())
                {
                    try
                    {
                        if (IsFallbackUserDefined)
                        {
                            WrapWithOnFallbackStartHook();
                            fallbackExecutionResult = ExecuteRunFallback();
                        }
                        else
                        {
                            // same logic as above without the hook invocation
                            fallbackExecutionResult = ExecuteRunFallback();
                        }

                        fallbackExecutionResult = WrapWithOnFallbackEmitHook(fallbackExecutionResult);
                        MarkFallbackEmit();
                        fallbackExecutionResult = WrapWithOnEmitHook(fallbackExecutionResult);
                        WrapWithOnFallbackSuccessHook();
                        MarkFallbackCompleted();
                    }
                    catch (Exception ex)
                    {
                        WrapWithOnFallbackErrorHook(ex);
                        HandleFallbackError(ex, failureType, message, originalException);

                        // Suppress S3626 to workaround bug at https://github.com/SonarSource/sonar-dotnet/issues/5691.
#pragma warning disable S3626 // Jump statements should not be redundant
                        return;
#pragma warning restore S3626 // Jump statements should not be redundant
                    }
                    finally
                    {
                        fallbackSemaphore?.Release();
                    }

                    tcs.TrySetResult(fallbackExecutionResult);
                }
                else
                {
                    HandleFallbackRejectionByEmittingError();
                }
            }
            else
            {
                HandleFallbackDisabledByEmittingError(originalException, failureType, message);
            }
        }
    }

    private void HandleFallbackError(Exception fe, FailureType failureType, string message, Exception originalException)
    {
        var e = originalException;

        if (fe is InvalidOperationException)
        {
            var latency = Time.CurrentTimeMillis - ExecutionResult.StartTimestamp;
            _logger?.LogDebug("No fallback for HystrixCommand: {0} ", fe); // debug only since we're throwing the exception and someone higher will do something with it
            EventNotifier.MarkEvent(HystrixEventType.FallbackMissing, InnerCommandKey);
            ExecutionResult = ExecutionResult.AddEvent((int)latency, HystrixEventType.FallbackMissing);

            /* executionHook for all errors */
            e = WrapWithOnErrorHook(failureType, e);

            tcs.TrySetException(new HystrixRuntimeException(
                failureType,
                GetType(),
                $"{LogMessagePrefix} {message} and no fallback available.",
                e,
                fe));
        }
        else
        {
            var latency = Time.CurrentTimeMillis - ExecutionResult.StartTimestamp;
            _logger?.LogDebug("HystrixCommand execution {0} and fallback failed: {1}", failureType.ToString(), fe);
            EventNotifier.MarkEvent(HystrixEventType.FallbackFailure, InnerCommandKey);
            ExecutionResult = ExecutionResult.AddEvent((int)latency, HystrixEventType.FallbackFailure);

            /* executionHook for all errors */
            e = WrapWithOnErrorHook(failureType, e);

            tcs.TrySetException(new HystrixRuntimeException(
                failureType,
                GetType(),
                $"{LogMessagePrefix} {message} and fallback failed.",
                e,
                fe));
        }
    }

    private void HandleFallbackDisabledByEmittingError(Exception underlying, FailureType failureType, string message)
    {
        /* fallback is disabled so throw HystrixRuntimeException */
        _logger?.LogDebug("Fallback disabled for HystrixCommand so will throw HystrixRuntimeException: {0} ", underlying); // debug only since we're throwing the exception and someone higher will do something with it

        /* executionHook for all errors */
        var wrapped = WrapWithOnErrorHook(failureType, underlying);
        tcs.TrySetException(new HystrixRuntimeException(
            failureType,
            GetType(),
            $"{LogMessagePrefix} {message} and fallback disabled.",
            wrapped,
            null));
    }

    private void HandleFallbackRejectionByEmittingError()
    {
        var latencyWithFallback = Time.CurrentTimeMillis - ExecutionResult.StartTimestamp;
        EventNotifier.MarkEvent(HystrixEventType.FallbackRejection, InnerCommandKey);
        ExecutionResult = ExecutionResult.AddEvent((int)latencyWithFallback, HystrixEventType.FallbackRejection);
        _logger?.LogDebug("HystrixCommand Fallback Rejection."); // debug only since we're throwing the exception and someone higher will do something with it

        // if we couldn't acquire a permit, we "fail fast" by throwing an exception
        tcs.TrySetException(new HystrixRuntimeException(
            FailureType.RejectedSemaphoreFallback,
            GetType(),
            $"{LogMessagePrefix} fallback execution rejected.",
            null,
            null));
    }

    private void HandleSemaphoreRejectionViaFallback()
    {
        var semaphoreRejectionException = new Exception("could not acquire a semaphore for execution");
        ExecutionResult = ExecutionResult.SetExecutionException(semaphoreRejectionException);
        EventNotifier.MarkEvent(HystrixEventType.SemaphoreRejected, InnerCommandKey);
        _logger?.LogDebug("HystrixCommand Execution Rejection by Semaphore."); // debug only since we're throwing the exception and someone higher will do something with it

        // retrieve a fallback or throw an exception if no fallback available
        HandleFallbackOrThrowException(
            HystrixEventType.SemaphoreRejected,
            FailureType.RejectedSemaphoreExecution,
            "could not acquire a semaphore for execution",
            semaphoreRejectionException);
    }

    private void HandleShortCircuitViaFallback()
    {
        // record that we are returning a short-circuited fallback
        EventNotifier.MarkEvent(HystrixEventType.ShortCircuited, InnerCommandKey);

        // short-circuit and go directly to fallback (or throw an exception if no fallback implemented)
        var shortCircuitException = new Exception("Hystrix circuit short-circuited and is OPEN");
        ExecutionResult = ExecutionResult.SetExecutionException(shortCircuitException);
        try
        {
            HandleFallbackOrThrowException(
                HystrixEventType.ShortCircuited,
                FailureType.Shortcircuit,
                "short-circuited",
                shortCircuitException);
        }
        catch (Exception e)
        {
            tcs.TrySetException(e);
        }
    }

    private void HandleFallback(Exception e)
    {
        if (e is TaskCanceledException ||
            e is OperationCanceledException)
        {
            // log
            tcs.TrySetCanceled();
            return;
        }

        if (e is TaskSchedulerException)
        {
            e = e.InnerException;
        }

        ExecutionResult = ExecutionResult.SetExecutionException(e);
        if (e is RejectedExecutionException)
        {
            HandleThreadPoolRejectionViaFallback(e);
        }
        else if (e is HystrixTimeoutException)
        {
            HandleTimeoutViaFallback();
        }
        else if (e is HystrixBadRequestException)
        {
            HandleBadRequestByEmittingError(e);
        }
        else
        {
            HandleFailureViaFallback(e);
        }
    }

    private void HandleFailureViaFallback(Exception underlying)
    {
        _logger?.LogDebug("Error executing HystrixCommand.Run(). Proceeding to fallback logic...: {0}", underlying);

        // report failure
        EventNotifier.MarkEvent(HystrixEventType.Failure, InnerCommandKey);

        // record the exception
        ExecutionResult = ExecutionResult.SetException(underlying);
        HandleFallbackOrThrowException(HystrixEventType.Failure, FailureType.CommandException, "failed", underlying);
    }

    private void HandleBadRequestByEmittingError(Exception underlying)
    {
        var toEmit = underlying;

        try
        {
            var executionLatency = Time.CurrentTimeMillis - ExecutionResult.StartTimestamp;
            EventNotifier.MarkEvent(HystrixEventType.BadRequest, InnerCommandKey);
            ExecutionResult = ExecutionResult.AddEvent((int)executionLatency, HystrixEventType.BadRequest);
            var decorated = ExecutionHook.OnError(this, FailureType.BadRequestException, underlying);

            if (decorated is HystrixBadRequestException)
            {
                toEmit = decorated;
            }
            else
            {
                _logger?.LogWarning("ExecutionHook.onError returned an exception that was not an instance of HystrixBadRequestException so will be ignored: {0}", decorated);
            }
        }
        catch (Exception hookEx)
        {
            _logger?.LogWarning("Error calling HystrixCommandExecutionHook.onError: {0}", hookEx);
        }

        tcs.TrySetException(toEmit);
    }

    private void HandleTimeoutViaFallback()
    {
        HandleFallbackOrThrowException(HystrixEventType.Timeout, FailureType.Timeout, "timed-out", new TimeoutException());
    }

    private void HandleThreadPoolRejectionViaFallback(Exception underlying)
    {
        EventNotifier.MarkEvent(HystrixEventType.ThreadPoolRejected, InnerCommandKey);
        ThreadPool.MarkThreadRejection();

        // use a fallback instead (or throw exception if not implemented)
        HandleFallbackOrThrowException(HystrixEventType.ThreadPoolRejected, FailureType.RejectedThreadExecution, "could not be queued for execution", underlying);
    }

    private void HandleRequestCacheHitAndEmitValues(Task fromCache, AbstractCommand<TResult> cmd)
    {
        try
        {
            ExecutionHook.OnCacheHit(this);
        }
        catch (Exception hookEx)
        {
            _logger?.LogWarning("Error calling HystrixCommandExecutionHook.onCacheHit: {0}", hookEx);
        }

        if (cmd.Token.IsCancellationRequested)
        {
            cmd.ExecutionResult = cmd.ExecutionResult.AddEvent(HystrixEventType.Cancelled);
            cmd.ExecutionResult = cmd.ExecutionResult.SetExecutionLatency(-1);
        }
        else
        {
            if (!fromCache.IsCompleted)
            {
                fromCache.Wait(cmd.Token);
            }

            if (fromCache.AsyncState is AbstractCommand<TResult> originalCommand)
            {
                cmd.ExecutionResult = originalCommand.ExecutionResult;
            }
        }

        if (cmd.Token.IsCancellationRequested)
        {
            if (InnerCommandState.CompareAndSet(CommandState.ObservableChainCreated, CommandState.Unsubscribed))
            {
                HandleCleanUpAfterResponseFromCache(false); // user code never ran
            }
            else if (InnerCommandState.CompareAndSet(CommandState.UserCodeExecuted, CommandState.Unsubscribed))
            {
                HandleCleanUpAfterResponseFromCache(true); // user code did run
            }
        }

        if (fromCache.IsCompleted)
        {
            if (InnerCommandState.CompareAndSet(CommandState.ObservableChainCreated, CommandState.Terminal))
            {
                HandleCleanUpAfterResponseFromCache(false); // user code never ran
            }
            else if (InnerCommandState.CompareAndSet(CommandState.UserCodeExecuted, CommandState.Terminal))
            {
                HandleCleanUpAfterResponseFromCache(true); // user code did run
            }
        }
    }

    private void TimeoutThreadAction()
    {
        if (!Time.WaitUntil(() => IsCommandTimedOut.Value == TimedOutStatus.Completed, InnerOptions.ExecutionTimeoutInMilliseconds))
        {
#pragma warning disable S1066 // Collapsible "if" statements should be merged
            if (IsCommandTimedOut.CompareAndSet(TimedOutStatus.NotExecuted, TimedOutStatus.TimedOut))
#pragma warning restore S1066 // Collapsible "if" statements should be merged
            {
                TimeoutTcs.Cancel();

                // report timeout failure
                EventNotifier.MarkEvent(HystrixEventType.Timeout, InnerCommandKey);

                if (InnerThreadState.CompareAndSet(ThreadState.Started, ThreadState.Unsubscribed))
                {
                    HandleThreadEnd();
                }

                InnerThreadState.CompareAndSet(ThreadState.NotUsingThread, ThreadState.Unsubscribed);

                HandleFallback(new HystrixTimeoutException("timed out while executing run()"));

                if (tcs.IsFaulted)
                {
                    EventNotifier.MarkEvent(HystrixEventType.ExceptionThrown, CommandKey);
                }

                UnsubscribeCommandCleanup();
                FireOnCompletedHook();
                TerminateCommandCleanup();
            }
        }
    }

    private void ExecuteCommandWithThreadAction()
    {
        ThreadStartTimestamp = Time.CurrentTimeMillis;

        if (Token.IsCancellationRequested)
        {
            tcs.TrySetCanceled();
            UnsubscribeCommandCleanup();
            InnerThreadState.CompareAndSet(ThreadState.NotUsingThread, ThreadState.Unsubscribed);
            TerminateCommandCleanup();
            return;
        }

        try
        {
            ExecutionResult = ExecutionResult.SetExecutionOccurred();
            if (!InnerCommandState.CompareAndSet(CommandState.ObservableChainCreated, CommandState.UserCodeExecuted))
            {
                tcs.TrySetException(new InvalidOperationException($"execution attempted while in state : {InnerCommandState.Value}"));
                return;
            }

            InnerMetrics.MarkCommandStart(InnerCommandKey, InnerThreadPoolKey, ExecutionIsolationStrategy.Thread);

            if (IsCommandTimedOut.Value == TimedOutStatus.TimedOut)
            {
                // the command timed out in the wrapping thread so we will return immediately
                // and not increment any of the counters below or other such logic
                tcs.TrySetException(new HystrixTimeoutException("timed out before executing run()"));
                return;
            }

            if (InnerThreadState.CompareAndSet(ThreadState.NotUsingThread, ThreadState.Started))
            {
                // we have not been unsubscribed, so should proceed
                HystrixCounters.IncrementGlobalConcurrentThreads();
                ThreadPool.MarkThreadExecution();

                // store the command that is being run
                ExecutionResult = ExecutionResult.SetExecutedInThread();
                /*
                 * If any of these hooks throw an exception, then it appears as if the actual execution threw an error
                 */
                try
                {
                    if (InnerOptions.ExecutionTimeoutEnabled)
                    {
                        var timerTask = new Task(TimeoutThreadAction, TaskCreationOptions.LongRunning);
                        timerTask.Start(TaskScheduler.Default);
                    }

                    ExecutionHook.OnThreadStart(this);
                    ExecutionHook.OnExecutionStart(this);
                    var result = ExecuteRun();
                    if (IsCommandTimedOut.Value != TimedOutStatus.TimedOut)
                    {
                        MarkEmits();
                        result = WrapWithOnEmitHook(result);
                        tcs.TrySetResult(result);
                        WrapWithOnExecutionSuccess();
                        if (InnerThreadState.CompareAndSet(ThreadState.Started, ThreadState.Terminal))
                        {
                            HandleThreadEnd();
                        }

                        InnerThreadState.CompareAndSet(ThreadState.NotUsingThread, ThreadState.Terminal);
                        MarkCompleted();
                    }
                }
                catch (Exception e)
                {
                    if (IsCommandTimedOut.Value != TimedOutStatus.TimedOut)
                    {
                        if (InnerThreadState.CompareAndSet(ThreadState.Started, ThreadState.Terminal))
                        {
                            HandleThreadEnd();
                        }

                        InnerThreadState.CompareAndSet(ThreadState.NotUsingThread, ThreadState.Terminal);
                        HandleFallback(e);
                    }
                }
            }
            else
            {
                // command has already been unsubscribed, so return immediately
                tcs.TrySetException(new Exception("unsubscribed before executing run()"));
            }
        }
        finally
        {
            if (IsCommandTimedOut.Value != TimedOutStatus.TimedOut)
            {
                // applyHystrixSemantics.doOnError(markexceptionthrown)
                if (tcs.IsFaulted)
                {
                    EventNotifier.MarkEvent(HystrixEventType.ExceptionThrown, CommandKey);
                }

                UnsubscribeCommandCleanup();
                FireOnCompletedHook();
                TerminateCommandCleanup();
            }
        }
    }

    private void ExecuteCommandWithSpecifiedIsolation()
    {
        if (InnerOptions.ExecutionIsolationStrategy == ExecutionIsolationStrategy.Thread)
        {
            void ThreadExec(object command)
            {
                ExecuteCommandWithThreadAction();
            }

            ExecThreadTask = new Task(ThreadExec, this, CancellationToken.None, TaskCreationOptions.LongRunning);
        }
        else
        {
            ExecutionResult = ExecutionResult.SetExecutionOccurred();
            if (!InnerCommandState.CompareAndSet(CommandState.ObservableChainCreated, CommandState.UserCodeExecuted))
            {
                throw new InvalidOperationException($"execution attempted while in state : {InnerCommandState.Value}");
            }

            InnerMetrics.MarkCommandStart(InnerCommandKey, InnerThreadPoolKey, ExecutionIsolationStrategy.Semaphore);

            if (InnerOptions.ExecutionTimeoutEnabled)
            {
                var timerTask = new Task(TimeoutThreadAction, TaskCreationOptions.LongRunning);
                timerTask.Start(TaskScheduler.Default);
            }

            try
            {
                ExecutionHook.OnExecutionStart(this);
                var result = ExecuteRun();
                if (IsCommandTimedOut.Value != TimedOutStatus.TimedOut)
                {
                    MarkEmits();
                    result = WrapWithOnEmitHook(result);
                    tcs.TrySetResult(result);
                    WrapWithOnExecutionSuccess();
                    MarkCompleted();
                }
            }
            catch (Exception e)
            {
                if (IsCommandTimedOut.Value != TimedOutStatus.TimedOut)
                {
                    HandleFallback(e);
                }
            }
        }
    }

    private TResult ExecuteRun()
    {
        try
        {
            var result = DoRun();
            IsCommandTimedOut.CompareAndSet(TimedOutStatus.NotExecuted, TimedOutStatus.Completed);

            result = IsCommandTimedOut.Value != TimedOutStatus.TimedOut ? WrapWithOnExecutionEmitHook(result) : default;

            return result;
        }
        catch (AggregateException e)
        {
            IsCommandTimedOut.CompareAndSet(TimedOutStatus.NotExecuted, TimedOutStatus.Completed);

            var flatten = GetException(e);
            if (flatten.InnerException is TaskCanceledException && IsCommandTimedOut.Value == TimedOutStatus.TimedOut)
            {
                // End task pass
                return default;
            }

            var ex = WrapWithOnExecutionErrorHook(flatten.InnerException);
            if (ex == flatten.InnerException)
            {
                throw;
            }

            throw ex;
        }
        catch (OperationCanceledException e)
        {
            IsCommandTimedOut.CompareAndSet(TimedOutStatus.NotExecuted, TimedOutStatus.Completed);

            if (IsCommandTimedOut.Value == TimedOutStatus.TimedOut)
            {
                // End task pass
                return default;
            }

            var ex = WrapWithOnExecutionErrorHook(e);
            if (e == ex)
            {
                throw;
            }

            throw ex;
        }
        catch (Exception ex)
        {
            IsCommandTimedOut.CompareAndSet(TimedOutStatus.NotExecuted, TimedOutStatus.Completed);

            var returned = WrapWithOnExecutionErrorHook(ex);
            if (ex == returned)
            {
                throw;
            }

            throw returned;
        }
    }

    private TResult ExecuteRunFallback()
    {
        try
        {
            return DoFallback();
        }
        catch (AggregateException ex)
        {
            var flatten = GetException(ex);
            throw flatten.InnerException;
        }
    }

    private bool IsUnrecoverableError(Exception t)
    {
        var cause = t;

        if (cause is OutOfMemoryException)
        {
            return true;
        }
        else if (cause is VerificationException)
        {
            return true;
        }
        else if (cause is InsufficientExecutionStackException)
        {
            return true;
        }
        else if (cause is BadImageFormatException)
        {
            return true;
        }

        return false;
    }

    private bool IsRecoverableError(Exception t)
    {
        return !IsUnrecoverableError(t);
    }

    private void UnsubscribeCommandCleanup()
    {
        if (tcs.IsCanceled)
        {
            if (InnerCommandState.CompareAndSet(CommandState.ObservableChainCreated, CommandState.Unsubscribed))
            {
                if (!ExecutionResult.ContainsTerminalEvent)
                {
                    EventNotifier.MarkEvent(HystrixEventType.Cancelled, CommandKey);
                    ExecutionResultAtTimeOfCancellation = ExecutionResult
                        .AddEvent((int)(Time.CurrentTimeMillis - CommandStartTimestamp), HystrixEventType.Cancelled);
                }

                HandleCommandEnd(false); // user code never ran
            }
            else if (InnerCommandState.CompareAndSet(CommandState.UserCodeExecuted, CommandState.Unsubscribed))
            {
                if (!ExecutionResult.ContainsTerminalEvent)
                {
                    EventNotifier.MarkEvent(HystrixEventType.Cancelled, CommandKey);
                    ExecutionResultAtTimeOfCancellation = ExecutionResult
                        .AddEvent((int)(Time.CurrentTimeMillis - CommandStartTimestamp), HystrixEventType.Cancelled);
                }

                HandleCommandEnd(true); // user code did run
            }
        }
    }

    private void TerminateCommandCleanup()
    {
        if (tcs.IsCompleted)
        {
            if (InnerCommandState.CompareAndSet(CommandState.ObservableChainCreated, CommandState.Terminal))
            {
                HandleCommandEnd(false); // user code never ran
            }
            else if (InnerCommandState.CompareAndSet(CommandState.UserCodeExecuted, CommandState.Terminal))
            {
                HandleCommandEnd(true); // user code did run
            }

            tcs.Commit();
        }
    }

    private void FireOnCompletedHook()
    {
        if (tcs.IsCompleted && !tcs.IsFaulted && !tcs.IsCanceled)
        {
            try
            {
                ExecutionHook.OnSuccess(this);
            }
            catch (Exception hookEx)
            {
                _logger?.LogWarning("Error calling HystrixCommandExecutionHook.onSuccess - {0}", hookEx);
            }
        }
    }

    private Exception GetException(AggregateException e)
    {
        return e.Flatten();
    }

    private void MarkCollapsedCommand(IHystrixCollapserKey collapserKey, int sizeOfBatch)
    {
        EventNotifier.MarkEvent(HystrixEventType.Collapsed, InnerCommandKey);
        ExecutionResult = ExecutionResult.MarkCollapsed(collapserKey, sizeOfBatch);
    }

    private void MarkFallbackEmit()
    {
        if (ShouldOutputOnNextEvents)
        {
            ExecutionResult = ExecutionResult.AddEvent(HystrixEventType.FallbackEmit);
            EventNotifier.MarkEvent(HystrixEventType.FallbackEmit, InnerCommandKey);
        }
    }

    private void MarkFallbackCompleted()
    {
        var latency2 = Time.CurrentTimeMillis - ExecutionResult.StartTimestamp;
        EventNotifier.MarkEvent(HystrixEventType.FallbackSuccess, InnerCommandKey);
        ExecutionResult = ExecutionResult.AddEvent((int)latency2, HystrixEventType.FallbackSuccess);
    }

    private void MarkEmits()
    {
        if (ShouldOutputOnNextEvents)
        {
            ExecutionResult = ExecutionResult.AddEvent(HystrixEventType.Emit);
            EventNotifier.MarkEvent(HystrixEventType.Emit, InnerCommandKey);
        }

        if (CommandIsScalar)
        {
            var latency = Time.CurrentTimeMillis - ExecutionResult.StartTimestamp;
            EventNotifier.MarkCommandExecution(CommandKey, InnerOptions.ExecutionIsolationStrategy, (int)latency, ExecutionResult.OrderedList);
            EventNotifier.MarkEvent(HystrixEventType.Success, InnerCommandKey);
            ExecutionResult = ExecutionResult.AddEvent((int)latency, HystrixEventType.Success);
            InnerCircuitBreaker.MarkSuccess();
        }
    }

    private void MarkCompleted()
    {
        if (tcs.IsCompleted && !CommandIsScalar)
        {
            var latency = Time.CurrentTimeMillis - ExecutionResult.StartTimestamp;
            EventNotifier.MarkCommandExecution(CommandKey, InnerOptions.ExecutionIsolationStrategy, (int)latency, ExecutionResult.OrderedList);
            EventNotifier.MarkEvent(HystrixEventType.Success, InnerCommandKey);
            ExecutionResult = ExecutionResult.AddEvent((int)latency, HystrixEventType.Success);
            InnerCircuitBreaker.MarkSuccess();
        }
    }

    private void WrapWithOnFallbackSuccessHook()
    {
        try
        {
            ExecutionHook.OnFallbackSuccess(this);
        }
        catch (Exception hookEx)
        {
            _logger?.LogWarning("Error calling HystrixCommandExecutionHook.onFallbackSuccess - {0}", hookEx);
        }
    }

    private TResult WrapWithOnFallbackEmitHook(TResult r)
    {
        try
        {
            return ExecutionHook.OnFallbackEmit(this, r);
        }
        catch (Exception hookEx)
        {
            _logger?.LogWarning("Error calling HystrixCommandExecutionHook.onFallbackEmit - {0}", hookEx);
            return r;
        }
    }

    private void WrapWithOnFallbackStartHook()
    {
        try
        {
            ExecutionHook.OnFallbackStart(this);
        }
        catch (Exception hookEx)
        {
            _logger?.LogWarning("Error calling HystrixCommandExecutionHook.OnFallbackStart - {0}", hookEx);
        }
    }

    private TResult WrapWithOnEmitHook(TResult result)
    {
        try
        {
            return ExecutionHook.OnEmit(this, result);
        }
        catch (Exception hookEx)
        {
            _logger?.LogWarning("Error calling HystrixCommandExecutionHook.onEmit - {0}", hookEx);
            return result;
        }
    }

    private Exception WrapWithOnErrorHook(FailureType failureType, Exception e)
    {
        try
        {
            return ExecutionHook.OnError(this, failureType, e);
        }
        catch (Exception hookEx)
        {
            _logger?.LogWarning("Error calling HystrixCommandExecutionHook.onError - {0}", hookEx);
            return e;
        }
    }

    private void WrapWithOnExecutionSuccess()
    {
        try
        {
            ExecutionHook.OnExecutionSuccess(this);
        }
        catch (Exception hookEx)
        {
            _logger?.LogWarning("Error calling HystrixCommandExecutionHook.onExecutionSuccess - {0}", hookEx);
        }
    }

    private Exception WrapWithOnExecutionErrorHook(Exception e)
    {
        try
        {
            return ExecutionHook.OnExecutionError(this, e);
        }
        catch (Exception hookEx)
        {
            _logger?.LogWarning("Error calling HystrixCommandExecutionHook.onExecutionError - {0}", hookEx);
            return e;
        }
    }

    private TResult WrapWithOnExecutionEmitHook(TResult r)
    {
        try
        {
            return ExecutionHook.OnExecutionEmit(this, r);
        }
        catch (Exception hookEx)
        {
            _logger?.LogWarning("Error calling HystrixCommandExecutionHook.onExecutionEmit - {0}", hookEx);
            return r;
        }
    }

    private void WrapWithOnFallbackErrorHook(Exception e)
    {
        try
        {
            if (IsFallbackUserDefined)
            {
                ExecutionHook.OnFallbackError(this, e);
            }
        }
        catch (Exception hookEx)
        {
            _logger?.LogWarning("Error calling HystrixCommandExecutionHook.onFallbackError - {0}", hookEx);
        }
    }

    public IHystrixCommandGroupKey CommandGroup => InnerCommandGroup;

    protected readonly IHystrixCommandKey InnerCommandKey;

    public IHystrixCommandKey CommandKey => InnerCommandKey;

    protected readonly IHystrixThreadPoolKey InnerThreadPoolKey;

    public IHystrixThreadPoolKey ThreadPoolKey => InnerThreadPoolKey;

    protected readonly IHystrixCommandOptions InnerOptions;

    public IHystrixCommandOptions CommandOptions => InnerOptions;

    public long CommandRunStartTimeInNanos => ExecutionResult.CommandRunStartTimeInNanos;

    public ExecutionResult.EventCounts EventCounts => CommandResult.Eventcounts;

    public List<HystrixEventType> ExecutionEvents => CommandResult.OrderedList;

    public int ExecutionTimeInMilliseconds => CommandResult.ExecutionLatency;

    public Exception FailedExecutionException => ExecutionResult.Exception;

    public bool IsCircuitBreakerOpen => InnerOptions.CircuitBreakerForceOpen || (!InnerOptions.CircuitBreakerForceClosed && InnerCircuitBreaker.IsOpen);

    public bool IsExecutedInThread => CommandResult.IsExecutedInThread;

    public bool IsExecutionComplete => InnerCommandState.Value == CommandState.Terminal;

    public bool IsFailedExecution => CommandResult.Eventcounts.Contains(HystrixEventType.Failure);

    public bool IsResponseFromCache => InnerIsResponseFromCache;

    public bool IsResponseFromFallback => CommandResult.Eventcounts.Contains(HystrixEventType.FallbackSuccess);

    public bool IsResponseRejected => CommandResult.IsResponseRejected;

    public bool IsResponseSemaphoreRejected => CommandResult.IsResponseSemaphoreRejected;

    public bool IsResponseShortCircuited => CommandResult.Eventcounts.Contains(HystrixEventType.ShortCircuited);

    public bool IsResponseThreadPoolRejected => CommandResult.IsResponseThreadPoolRejected;

    public bool IsResponseTimedOut => CommandResult.Eventcounts.Contains(HystrixEventType.Timeout);

    public bool IsSuccessfulExecution => CommandResult.Eventcounts.Contains(HystrixEventType.Success);

    public HystrixCommandMetrics Metrics => InnerMetrics;

    public int NumberCollapsed => CommandResult.Eventcounts.GetCount(HystrixEventType.Collapsed);

    public int NumberEmissions => CommandResult.Eventcounts.GetCount(HystrixEventType.Emit);

    public int NumberFallbackEmissions => CommandResult.Eventcounts.GetCount(HystrixEventType.FallbackEmit);

    public IHystrixCollapserKey OriginatingCollapserKey => ExecutionResult.CollapserKey;

    public string PublicCacheKey => CacheKey;

    protected bool innerIsFallbackUserDefined;

    public virtual bool IsFallbackUserDefined
    {
        get => innerIsFallbackUserDefined;

        set => innerIsFallbackUserDefined = value;
    }

    public Exception ExecutionException => ExecutionResult.ExecutionException;

    internal ICircuitBreaker CircuitBreaker => InnerCircuitBreaker;

    protected virtual string CacheKey => null;

    protected virtual bool IsRequestCachingEnabled => InnerOptions.RequestCacheEnabled && CacheKey != null;

    protected virtual string LogMessagePrefix => CommandKey.Name;

    protected virtual ExecutionResult CommandResult
    {
        get
        {
            var resultToReturn = ExecutionResultAtTimeOfCancellation ?? ExecutionResult;

            if (InnerIsResponseFromCache)
            {
                resultToReturn = resultToReturn.AddEvent(HystrixEventType.ResponseFromCache);
            }

            return resultToReturn;
        }
    }

    protected virtual bool ShouldOutputOnNextEvents => false;

    protected virtual bool CommandIsScalar => true;
}
