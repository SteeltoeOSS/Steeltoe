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
    #region NestedTypes
    protected enum TimedOutStatus
    {
        NOT_EXECUTED,
        COMPLETED,
        TIMED_OUT
    }

    protected enum CommandState
    {
        NOT_STARTED,
        OBSERVABLE_CHAIN_CREATED,
        USER_CODE_EXECUTED,
        UNSUBSCRIBED,
        TERMINAL
    }

    protected enum ThreadState
    {
        NOT_USING_THREAD,
        STARTED,
        UNSUBSCRIBED,
        TERMINAL
    }

    protected class AtomicCommandState : AtomicInteger
    {
        public AtomicCommandState(CommandState state)
            : base((int)state)
        {
        }

        public new CommandState Value
        {
            get => (CommandState)_value;

            set => _value = (int)value;
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
            get => (ThreadState)_value;

            set => _value = (int)value;
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
            get => (TimedOutStatus)_value;

            set => _value = (int)value;
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

    #endregion NestedTypes

    #region Fields
    protected internal readonly HystrixRequestLog _currentRequestLog;
    protected internal readonly HystrixRequestCache _requestCache;
    protected internal readonly HystrixCommandExecutionHook _executionHook;
    protected internal readonly HystrixCommandMetrics _metrics;
    protected internal readonly HystrixEventNotifier _eventNotifier;
    protected internal readonly ICircuitBreaker _circuitBreaker;
    protected internal readonly IHystrixThreadPool _threadPool;
    protected internal readonly SemaphoreSlim _fallbackSemaphoreOverride;
    protected internal readonly SemaphoreSlim _executionSemaphoreOverride;
    protected internal readonly HystrixConcurrencyStrategy _concurrencyStrategy;
    protected internal long _commandStartTimestamp = -1L;
    protected internal long _threadStartTimestamp = -1L;
    protected internal volatile bool _isResponseFromCache;
    protected internal Task _execThreadTask;
    protected internal CancellationTokenSource _timeoutTcs;
    protected internal CancellationToken _token;
    protected internal CancellationToken _usersToken;
    protected internal volatile ExecutionResult _executionResult = EMPTY; // state on shared execution
    protected internal volatile ExecutionResult _executionResultAtTimeOfCancellation;

    protected readonly AtomicCommandState commandState = new (CommandState.NOT_STARTED);
    protected readonly AtomicThreadState threadState = new (ThreadState.NOT_USING_THREAD);
    protected readonly AtomicTimedOutStatus isCommandTimedOut = new (TimedOutStatus.NOT_EXECUTED);
    protected readonly IHystrixCommandGroupKey commandGroup;

    protected HystrixCompletionSource tcs;
    private readonly ILogger _logger;

    #endregion Fields

    #region Constructors
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
        commandGroup = InitGroupKey(group);
        commandKey = InitCommandKey(key, GetType());
        options = InitCommandOptions(commandKey, optionsStrategy, commandOptionsDefaults);
        this.threadPoolKey = InitThreadPoolKey(threadPoolKey, commandGroup, options.ExecutionIsolationThreadPoolKeyOverride);
        _metrics = InitMetrics(metrics, commandGroup, this.threadPoolKey, commandKey, options);
        _circuitBreaker = InitCircuitBreaker(options.CircuitBreakerEnabled, circuitBreaker, commandGroup, commandKey, options, _metrics);

        _threadPool = InitThreadPool(threadPool, this.threadPoolKey, threadPoolOptionsDefaults);

        // Strategies from plugins
        _eventNotifier = HystrixPlugins.EventNotifier;
        _concurrencyStrategy = HystrixPlugins.ConcurrencyStrategy;
        HystrixMetricsPublisherFactory.CreateOrRetrievePublisherForCommand(commandKey, commandGroup, _metrics, _circuitBreaker, options);

        _executionHook = InitExecutionHook(executionHook);

        _requestCache = HystrixRequestCache.GetInstance(commandKey);
        _currentRequestLog = InitRequestLog(options.RequestLogEnabled);

        /* fallback semaphore override if applicable */
        _fallbackSemaphoreOverride = fallbackSemaphore;

        /* execution semaphore override if applicable */
        _executionSemaphoreOverride = executionSemaphore;
    }
    #endregion Constructors

    internal void MarkAsCollapsedCommand(IHystrixCollapserKey collapserKey, int sizeOfBatch)
    {
        MarkCollapsedCommand(collapserKey, sizeOfBatch);
    }

    protected internal SemaphoreSlim GetExecutionSemaphore()
    {
        if (options.ExecutionIsolationStrategy == ExecutionIsolationStrategy.SEMAPHORE)
        {
            if (_executionSemaphoreOverride == null)
            {
                return _executionSemaphorePerCircuit.GetOrAddEx(commandKey.Name, k => new SemaphoreSlim(options.ExecutionIsolationSemaphoreMaxConcurrentRequests));
            }
            else
            {
                return _executionSemaphoreOverride;
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
        if (_fallbackSemaphoreOverride == null)
        {
            return _fallbackSemaphorePerCircuit.GetOrAddEx(commandKey.Name, k => new SemaphoreSlim(options.FallbackIsolationSemaphoreMaxConcurrentRequests));
        }
        else
        {
            return _fallbackSemaphoreOverride;
        }
    }

    #region  Init
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
    #endregion

    protected void Setup()
    {
        _timeoutTcs = CancellationTokenSource.CreateLinkedTokenSource(_usersToken);
        _token = _timeoutTcs.Token;
        tcs = new HystrixCompletionSource(this);

        /* this is a stateful object so can only be used once */
        if (!commandState.CompareAndSet(CommandState.NOT_STARTED, CommandState.OBSERVABLE_CHAIN_CREATED))
        {
            var ex = new InvalidOperationException("This instance can only be executed once. Please instantiate a new instance.");
            throw new HystrixRuntimeException(
                FailureType.BAD_REQUEST_EXCEPTION,
                GetType(),
                $"{LogMessagePrefix} command executed multiple times - this is not permitted.",
                ex,
                null);
        }

        _commandStartTimestamp = Time.CurrentTimeMillis;

        if (CommandOptions.RequestLogEnabled && _currentRequestLog != null)
        {
            // log this command execution regardless of what happened
            _currentRequestLog.AddExecutedCommand(this);
        }
    }

    protected bool PutInCacheIfAbsent(Task<TResult> hystrixTask, out Task<TResult> fromCache)
    {
        fromCache = null;
        if (IsRequestCachingEnabled && CacheKey != null)
        {
            // wrap it for caching
            fromCache = _requestCache.PutIfAbsent(CacheKey, hystrixTask);
            if (fromCache != null)
            {
                // another thread beat us so we'll use the cached value instead
                _isResponseFromCache = true;
                HandleRequestCacheHitAndEmitValues(fromCache, this);
                return true;
            }
        }

        return false;
    }

    protected void ApplyHystrixSemantics()
    {
        if (commandState.Value.Equals(CommandState.UNSUBSCRIBED))
        {
            return;
        }

        try
        {
            // mark that we're starting execution on the ExecutionHook
            // if this hook throws an exception, then a fast-fail occurs with no fallback.  No state is left inconsistent
            _executionHook.OnStart(this);

            /* determine if we're allowed to execute */
            if (_circuitBreaker.AllowRequest)
            {
                var executionSemaphore = GetExecutionSemaphore();

                if (executionSemaphore.TryAcquire())
                {
                    try
                    {
                        /* used to track userThreadExecutionTime */
                        _executionResult = _executionResult.SetInvocationStartTime(Time.CurrentTimeMillis);
                        ExecuteCommandWithSpecifiedIsolation();
                        if (tcs.IsFaulted)
                        {
                            _eventNotifier.MarkEvent(HystrixEventType.EXCEPTION_THROWN, CommandKey);
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
                    _eventNotifier.MarkEvent(HystrixEventType.EXCEPTION_THROWN, CommandKey);
                }
            }
        }
        finally
        {
            if (isCommandTimedOut.Value != TimedOutStatus.TIMED_OUT)
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
            _execThreadTask.Start(_threadPool.GetTaskScheduler());
        }
        catch (Exception e)
        {
            HandleFallback(e);
            if (tcs.IsFaulted)
            {
                _eventNotifier.MarkEvent(HystrixEventType.EXCEPTION_THROWN, CommandKey);
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
        return new HystrixRuntimeException(FailureType.COMMAND_EXCEPTION, GetType(), message, e, null);
    }

    protected abstract TResult DoRun();

    protected abstract TResult DoFallback();

    #region Handlers
    protected virtual void HandleCleanUpAfterResponseFromCache(bool commandExecutionStarted)
    {
        var latency = Time.CurrentTimeMillis - _commandStartTimestamp;
        _executionResult = _executionResult.AddEvent(-1, HystrixEventType.RESPONSE_FROM_CACHE)
            .MarkUserThreadCompletion(latency)
            .SetNotExecutedInThread();
        var cacheOnlyForMetrics = ExecutionResult.From(HystrixEventType.RESPONSE_FROM_CACHE)
            .MarkUserThreadCompletion(latency);
        _metrics.MarkCommandDone(cacheOnlyForMetrics, commandKey, threadPoolKey, commandExecutionStarted);
        _eventNotifier.MarkEvent(HystrixEventType.RESPONSE_FROM_CACHE, commandKey);
    }

    protected virtual void HandleCommandEnd(bool commandExecutionStarted)
    {
        var userThreadLatency = Time.CurrentTimeMillis - _commandStartTimestamp;
        _executionResult = _executionResult.MarkUserThreadCompletion((int)userThreadLatency);
        _metrics.MarkCommandDone(_executionResultAtTimeOfCancellation ?? _executionResult, commandKey, threadPoolKey, commandExecutionStarted);
    }

    protected virtual void HandleThreadEnd()
    {
        HystrixCounters.DecrementGlobalConcurrentThreads();
        _threadPool.MarkThreadCompletion();
        try
        {
            _executionHook.OnThreadComplete(this);
        }
        catch (Exception hookEx)
        {
            _logger?.LogWarning("Error calling HystrixCommandExecutionHook.onThreadComplete: {0}", hookEx);
        }
    }

    private void HandleFallbackOrThrowException(HystrixEventType eventType, FailureType failureType, string message, Exception originalException)
    {
        var latency = Time.CurrentTimeMillis - _executionResult.StartTimestamp;

        // record the executionResult
        // do this before executing fallback so it can be queried from within getFallback (see See https://github.com/Netflix/Hystrix/pull/144)
        _executionResult = _executionResult.AddEvent((int)latency, eventType);

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

            if (options.FallbackEnabled)
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
            var latency = Time.CurrentTimeMillis - _executionResult.StartTimestamp;
            _logger?.LogDebug("No fallback for HystrixCommand: {0} ", fe); // debug only since we're throwing the exception and someone higher will do something with it
            _eventNotifier.MarkEvent(HystrixEventType.FALLBACK_MISSING, commandKey);
            _executionResult = _executionResult.AddEvent((int)latency, HystrixEventType.FALLBACK_MISSING);

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
            var latency = Time.CurrentTimeMillis - _executionResult.StartTimestamp;
            _logger?.LogDebug("HystrixCommand execution {0} and fallback failed: {1}", failureType.ToString(), fe);
            _eventNotifier.MarkEvent(HystrixEventType.FALLBACK_FAILURE, commandKey);
            _executionResult = _executionResult.AddEvent((int)latency, HystrixEventType.FALLBACK_FAILURE);

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
        var latencyWithFallback = Time.CurrentTimeMillis - _executionResult.StartTimestamp;
        _eventNotifier.MarkEvent(HystrixEventType.FALLBACK_REJECTION, commandKey);
        _executionResult = _executionResult.AddEvent((int)latencyWithFallback, HystrixEventType.FALLBACK_REJECTION);
        _logger?.LogDebug("HystrixCommand Fallback Rejection."); // debug only since we're throwing the exception and someone higher will do something with it

        // if we couldn't acquire a permit, we "fail fast" by throwing an exception
        tcs.TrySetException(new HystrixRuntimeException(
            FailureType.REJECTED_SEMAPHORE_FALLBACK,
            GetType(),
            $"{LogMessagePrefix} fallback execution rejected.",
            null,
            null));
    }

    private void HandleSemaphoreRejectionViaFallback()
    {
        var semaphoreRejectionException = new Exception("could not acquire a semaphore for execution");
        _executionResult = _executionResult.SetExecutionException(semaphoreRejectionException);
        _eventNotifier.MarkEvent(HystrixEventType.SEMAPHORE_REJECTED, commandKey);
        _logger?.LogDebug("HystrixCommand Execution Rejection by Semaphore."); // debug only since we're throwing the exception and someone higher will do something with it

        // retrieve a fallback or throw an exception if no fallback available
        HandleFallbackOrThrowException(
            HystrixEventType.SEMAPHORE_REJECTED,
            FailureType.REJECTED_SEMAPHORE_EXECUTION,
            "could not acquire a semaphore for execution",
            semaphoreRejectionException);
    }

    private void HandleShortCircuitViaFallback()
    {
        // record that we are returning a short-circuited fallback
        _eventNotifier.MarkEvent(HystrixEventType.SHORT_CIRCUITED, commandKey);

        // short-circuit and go directly to fallback (or throw an exception if no fallback implemented)
        var shortCircuitException = new Exception("Hystrix circuit short-circuited and is OPEN");
        _executionResult = _executionResult.SetExecutionException(shortCircuitException);
        try
        {
            HandleFallbackOrThrowException(
                HystrixEventType.SHORT_CIRCUITED,
                FailureType.SHORTCIRCUIT,
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

        _executionResult = _executionResult.SetExecutionException(e);
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
        _eventNotifier.MarkEvent(HystrixEventType.FAILURE, commandKey);

        // record the exception
        _executionResult = _executionResult.SetException(underlying);
        HandleFallbackOrThrowException(HystrixEventType.FAILURE, FailureType.COMMAND_EXCEPTION, "failed", underlying);
    }

    private void HandleBadRequestByEmittingError(Exception underlying)
    {
        var toEmit = underlying;

        try
        {
            var executionLatency = Time.CurrentTimeMillis - _executionResult.StartTimestamp;
            _eventNotifier.MarkEvent(HystrixEventType.BAD_REQUEST, commandKey);
            _executionResult = _executionResult.AddEvent((int)executionLatency, HystrixEventType.BAD_REQUEST);
            var decorated = _executionHook.OnError(this, FailureType.BAD_REQUEST_EXCEPTION, underlying);

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
        HandleFallbackOrThrowException(HystrixEventType.TIMEOUT, FailureType.TIMEOUT, "timed-out", new TimeoutException());
    }

    private void HandleThreadPoolRejectionViaFallback(Exception underlying)
    {
        _eventNotifier.MarkEvent(HystrixEventType.THREAD_POOL_REJECTED, commandKey);
        _threadPool.MarkThreadRejection();

        // use a fallback instead (or throw exception if not implemented)
        HandleFallbackOrThrowException(HystrixEventType.THREAD_POOL_REJECTED, FailureType.REJECTED_THREAD_EXECUTION, "could not be queued for execution", underlying);
    }

    private void HandleRequestCacheHitAndEmitValues(Task fromCache, AbstractCommand<TResult> cmd)
    {
        try
        {
            _executionHook.OnCacheHit(this);
        }
        catch (Exception hookEx)
        {
            _logger?.LogWarning("Error calling HystrixCommandExecutionHook.onCacheHit: {0}", hookEx);
        }

        if (cmd._token.IsCancellationRequested)
        {
            cmd._executionResult = cmd._executionResult.AddEvent(HystrixEventType.CANCELLED);
            cmd._executionResult = cmd._executionResult.SetExecutionLatency(-1);
        }
        else
        {
            if (!fromCache.IsCompleted)
            {
                fromCache.Wait(cmd._token);
            }

            if (fromCache.AsyncState is AbstractCommand<TResult> originalCommand)
            {
                cmd._executionResult = originalCommand._executionResult;
            }
        }

        if (cmd._token.IsCancellationRequested)
        {
            if (commandState.CompareAndSet(CommandState.OBSERVABLE_CHAIN_CREATED, CommandState.UNSUBSCRIBED))
            {
                HandleCleanUpAfterResponseFromCache(false); // user code never ran
            }
            else if (commandState.CompareAndSet(CommandState.USER_CODE_EXECUTED, CommandState.UNSUBSCRIBED))
            {
                HandleCleanUpAfterResponseFromCache(true); // user code did run
            }
        }

        if (fromCache.IsCompleted)
        {
            if (commandState.CompareAndSet(CommandState.OBSERVABLE_CHAIN_CREATED, CommandState.TERMINAL))
            {
                HandleCleanUpAfterResponseFromCache(false); // user code never ran
            }
            else if (commandState.CompareAndSet(CommandState.USER_CODE_EXECUTED, CommandState.TERMINAL))
            {
                HandleCleanUpAfterResponseFromCache(true); // user code did run
            }
        }
    }

    #endregion Handlers

    private void TimeoutThreadAction()
    {
        if (!Time.WaitUntil(() => isCommandTimedOut.Value == TimedOutStatus.COMPLETED, options.ExecutionTimeoutInMilliseconds))
        {
#pragma warning disable S1066 // Collapsible "if" statements should be merged
            if (isCommandTimedOut.CompareAndSet(TimedOutStatus.NOT_EXECUTED, TimedOutStatus.TIMED_OUT))
#pragma warning restore S1066 // Collapsible "if" statements should be merged
            {
                _timeoutTcs.Cancel();

                // report timeout failure
                _eventNotifier.MarkEvent(HystrixEventType.TIMEOUT, commandKey);

                if (threadState.CompareAndSet(ThreadState.STARTED, ThreadState.UNSUBSCRIBED))
                {
                    HandleThreadEnd();
                }

                threadState.CompareAndSet(ThreadState.NOT_USING_THREAD, ThreadState.UNSUBSCRIBED);

                HandleFallback(new HystrixTimeoutException("timed out while executing run()"));

                if (tcs.IsFaulted)
                {
                    _eventNotifier.MarkEvent(HystrixEventType.EXCEPTION_THROWN, CommandKey);
                }

                UnsubscribeCommandCleanup();
                FireOnCompletedHook();
                TerminateCommandCleanup();
            }
        }
    }

    private void ExecuteCommandWithThreadAction()
    {
        _threadStartTimestamp = Time.CurrentTimeMillis;

        if (_token.IsCancellationRequested)
        {
            tcs.TrySetCanceled();
            UnsubscribeCommandCleanup();
            threadState.CompareAndSet(ThreadState.NOT_USING_THREAD, ThreadState.UNSUBSCRIBED);
            TerminateCommandCleanup();
            return;
        }

        try
        {
            _executionResult = _executionResult.SetExecutionOccurred();
            if (!commandState.CompareAndSet(CommandState.OBSERVABLE_CHAIN_CREATED, CommandState.USER_CODE_EXECUTED))
            {
                tcs.TrySetException(new InvalidOperationException($"execution attempted while in state : {commandState.Value}"));
                return;
            }

            _metrics.MarkCommandStart(commandKey, threadPoolKey, ExecutionIsolationStrategy.THREAD);

            if (isCommandTimedOut.Value == TimedOutStatus.TIMED_OUT)
            {
                // the command timed out in the wrapping thread so we will return immediately
                // and not increment any of the counters below or other such logic
                tcs.TrySetException(new HystrixTimeoutException("timed out before executing run()"));
                return;
            }

            if (threadState.CompareAndSet(ThreadState.NOT_USING_THREAD, ThreadState.STARTED))
            {
                // we have not been unsubscribed, so should proceed
                HystrixCounters.IncrementGlobalConcurrentThreads();
                _threadPool.MarkThreadExecution();

                // store the command that is being run
                _executionResult = _executionResult.SetExecutedInThread();
                /*
                 * If any of these hooks throw an exception, then it appears as if the actual execution threw an error
                 */
                try
                {
                    if (options.ExecutionTimeoutEnabled)
                    {
                        var timerTask = new Task(TimeoutThreadAction, TaskCreationOptions.LongRunning);
                        timerTask.Start(TaskScheduler.Default);
                    }

                    _executionHook.OnThreadStart(this);
                    _executionHook.OnExecutionStart(this);
                    var result = ExecuteRun();
                    if (isCommandTimedOut.Value != TimedOutStatus.TIMED_OUT)
                    {
                        MarkEmits();
                        result = WrapWithOnEmitHook(result);
                        tcs.TrySetResult(result);
                        WrapWithOnExecutionSuccess();
                        if (threadState.CompareAndSet(ThreadState.STARTED, ThreadState.TERMINAL))
                        {
                            HandleThreadEnd();
                        }

                        threadState.CompareAndSet(ThreadState.NOT_USING_THREAD, ThreadState.TERMINAL);
                        MarkCompleted();
                    }
                }
                catch (Exception e)
                {
                    if (isCommandTimedOut.Value != TimedOutStatus.TIMED_OUT)
                    {
                        if (threadState.CompareAndSet(ThreadState.STARTED, ThreadState.TERMINAL))
                        {
                            HandleThreadEnd();
                        }

                        threadState.CompareAndSet(ThreadState.NOT_USING_THREAD, ThreadState.TERMINAL);
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
            if (isCommandTimedOut.Value != TimedOutStatus.TIMED_OUT)
            {
                // applyHystrixSemantics.doOnError(markexceptionthrown)
                if (tcs.IsFaulted)
                {
                    _eventNotifier.MarkEvent(HystrixEventType.EXCEPTION_THROWN, CommandKey);
                }

                UnsubscribeCommandCleanup();
                FireOnCompletedHook();
                TerminateCommandCleanup();
            }
        }
    }

    private void ExecuteCommandWithSpecifiedIsolation()
    {
        if (options.ExecutionIsolationStrategy == ExecutionIsolationStrategy.THREAD)
        {
            void ThreadExec(object command)
            {
                ExecuteCommandWithThreadAction();
            }

            _execThreadTask = new Task(ThreadExec, this, CancellationToken.None, TaskCreationOptions.LongRunning);
        }
        else
        {
            _executionResult = _executionResult.SetExecutionOccurred();
            if (!commandState.CompareAndSet(CommandState.OBSERVABLE_CHAIN_CREATED, CommandState.USER_CODE_EXECUTED))
            {
                throw new InvalidOperationException($"execution attempted while in state : {commandState.Value}");
            }

            _metrics.MarkCommandStart(commandKey, threadPoolKey, ExecutionIsolationStrategy.SEMAPHORE);

            if (options.ExecutionTimeoutEnabled)
            {
                var timerTask = new Task(TimeoutThreadAction, TaskCreationOptions.LongRunning);
                timerTask.Start(TaskScheduler.Default);
            }

            try
            {
                _executionHook.OnExecutionStart(this);
                var result = ExecuteRun();
                if (isCommandTimedOut.Value != TimedOutStatus.TIMED_OUT)
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
                if (isCommandTimedOut.Value != TimedOutStatus.TIMED_OUT)
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
            isCommandTimedOut.CompareAndSet(TimedOutStatus.NOT_EXECUTED, TimedOutStatus.COMPLETED);

            result = isCommandTimedOut.Value != TimedOutStatus.TIMED_OUT ? WrapWithOnExecutionEmitHook(result) : default;

            return result;
        }
        catch (AggregateException e)
        {
            isCommandTimedOut.CompareAndSet(TimedOutStatus.NOT_EXECUTED, TimedOutStatus.COMPLETED);

            var flatten = GetException(e);
            if (flatten.InnerException is TaskCanceledException && isCommandTimedOut.Value == TimedOutStatus.TIMED_OUT)
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
            isCommandTimedOut.CompareAndSet(TimedOutStatus.NOT_EXECUTED, TimedOutStatus.COMPLETED);

            if (isCommandTimedOut.Value == TimedOutStatus.TIMED_OUT)
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
            isCommandTimedOut.CompareAndSet(TimedOutStatus.NOT_EXECUTED, TimedOutStatus.COMPLETED);

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
            if (commandState.CompareAndSet(CommandState.OBSERVABLE_CHAIN_CREATED, CommandState.UNSUBSCRIBED))
            {
                if (!_executionResult.ContainsTerminalEvent)
                {
                    _eventNotifier.MarkEvent(HystrixEventType.CANCELLED, CommandKey);
                    _executionResultAtTimeOfCancellation = _executionResult
                        .AddEvent((int)(Time.CurrentTimeMillis - _commandStartTimestamp), HystrixEventType.CANCELLED);
                }

                HandleCommandEnd(false); // user code never ran
            }
            else if (commandState.CompareAndSet(CommandState.USER_CODE_EXECUTED, CommandState.UNSUBSCRIBED))
            {
                if (!_executionResult.ContainsTerminalEvent)
                {
                    _eventNotifier.MarkEvent(HystrixEventType.CANCELLED, CommandKey);
                    _executionResultAtTimeOfCancellation = _executionResult
                        .AddEvent((int)(Time.CurrentTimeMillis - _commandStartTimestamp), HystrixEventType.CANCELLED);
                }

                HandleCommandEnd(true); // user code did run
            }
        }
    }

    private void TerminateCommandCleanup()
    {
        if (tcs.IsCompleted)
        {
            if (commandState.CompareAndSet(CommandState.OBSERVABLE_CHAIN_CREATED, CommandState.TERMINAL))
            {
                HandleCommandEnd(false); // user code never ran
            }
            else if (commandState.CompareAndSet(CommandState.USER_CODE_EXECUTED, CommandState.TERMINAL))
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
                _executionHook.OnSuccess(this);
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

    #region Marks

    private void MarkCollapsedCommand(IHystrixCollapserKey collapserKey, int sizeOfBatch)
    {
        _eventNotifier.MarkEvent(HystrixEventType.COLLAPSED, commandKey);
        _executionResult = _executionResult.MarkCollapsed(collapserKey, sizeOfBatch);
    }

    private void MarkFallbackEmit()
    {
        if (ShouldOutputOnNextEvents)
        {
            _executionResult = _executionResult.AddEvent(HystrixEventType.FALLBACK_EMIT);
            _eventNotifier.MarkEvent(HystrixEventType.FALLBACK_EMIT, commandKey);
        }
    }

    private void MarkFallbackCompleted()
    {
        var latency2 = Time.CurrentTimeMillis - _executionResult.StartTimestamp;
        _eventNotifier.MarkEvent(HystrixEventType.FALLBACK_SUCCESS, commandKey);
        _executionResult = _executionResult.AddEvent((int)latency2, HystrixEventType.FALLBACK_SUCCESS);
    }

    private void MarkEmits()
    {
        if (ShouldOutputOnNextEvents)
        {
            _executionResult = _executionResult.AddEvent(HystrixEventType.EMIT);
            _eventNotifier.MarkEvent(HystrixEventType.EMIT, commandKey);
        }

        if (CommandIsScalar)
        {
            var latency = Time.CurrentTimeMillis - _executionResult.StartTimestamp;
            _eventNotifier.MarkCommandExecution(CommandKey, options.ExecutionIsolationStrategy, (int)latency, _executionResult.OrderedList);
            _eventNotifier.MarkEvent(HystrixEventType.SUCCESS, commandKey);
            _executionResult = _executionResult.AddEvent((int)latency, HystrixEventType.SUCCESS);
            _circuitBreaker.MarkSuccess();
        }
    }

    private void MarkCompleted()
    {
        if (tcs.IsCompleted && !CommandIsScalar)
        {
            var latency = Time.CurrentTimeMillis - _executionResult.StartTimestamp;
            _eventNotifier.MarkCommandExecution(CommandKey, options.ExecutionIsolationStrategy, (int)latency, _executionResult.OrderedList);
            _eventNotifier.MarkEvent(HystrixEventType.SUCCESS, commandKey);
            _executionResult = _executionResult.AddEvent((int)latency, HystrixEventType.SUCCESS);
            _circuitBreaker.MarkSuccess();
        }
    }

    #endregion Marks

    #region Wraps
    private void WrapWithOnFallbackSuccessHook()
    {
        try
        {
            _executionHook.OnFallbackSuccess(this);
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
            return _executionHook.OnFallbackEmit(this, r);
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
            _executionHook.OnFallbackStart(this);
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
            return _executionHook.OnEmit(this, result);
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
            return _executionHook.OnError(this, failureType, e);
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
            _executionHook.OnExecutionSuccess(this);
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
            return _executionHook.OnExecutionError(this, e);
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
            return _executionHook.OnExecutionEmit(this, r);
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
                _executionHook.OnFallbackError(this, e);
            }
        }
        catch (Exception hookEx)
        {
            _logger?.LogWarning("Error calling HystrixCommandExecutionHook.onFallbackError - {0}", hookEx);
        }
    }
    #endregion Wraps

    #region IHystrixInvokableInfo

    public IHystrixCommandGroupKey CommandGroup => commandGroup;

    protected readonly IHystrixCommandKey commandKey;

    public IHystrixCommandKey CommandKey => commandKey;

    protected readonly IHystrixThreadPoolKey threadPoolKey;

    public IHystrixThreadPoolKey ThreadPoolKey => threadPoolKey;

    protected readonly IHystrixCommandOptions options;

    public IHystrixCommandOptions CommandOptions => options;

    public long CommandRunStartTimeInNanos => _executionResult.CommandRunStartTimeInNanos;

    public ExecutionResult.EventCounts EventCounts => CommandResult.Eventcounts;

    public List<HystrixEventType> ExecutionEvents => CommandResult.OrderedList;

    public int ExecutionTimeInMilliseconds => CommandResult.ExecutionLatency;

    public Exception FailedExecutionException => _executionResult.Exception;

    public bool IsCircuitBreakerOpen => options.CircuitBreakerForceOpen || (!options.CircuitBreakerForceClosed && _circuitBreaker.IsOpen);

    public bool IsExecutedInThread => CommandResult.IsExecutedInThread;

    public bool IsExecutionComplete => commandState.Value == CommandState.TERMINAL;

    public bool IsFailedExecution => CommandResult.Eventcounts.Contains(HystrixEventType.FAILURE);

    public bool IsResponseFromCache => _isResponseFromCache;

    public bool IsResponseFromFallback => CommandResult.Eventcounts.Contains(HystrixEventType.FALLBACK_SUCCESS);

    public bool IsResponseRejected => CommandResult.IsResponseRejected;

    public bool IsResponseSemaphoreRejected => CommandResult.IsResponseSemaphoreRejected;

    public bool IsResponseShortCircuited => CommandResult.Eventcounts.Contains(HystrixEventType.SHORT_CIRCUITED);

    public bool IsResponseThreadPoolRejected => CommandResult.IsResponseThreadPoolRejected;

    public bool IsResponseTimedOut => CommandResult.Eventcounts.Contains(HystrixEventType.TIMEOUT);

    public bool IsSuccessfulExecution => CommandResult.Eventcounts.Contains(HystrixEventType.SUCCESS);

    public HystrixCommandMetrics Metrics => _metrics;

    public int NumberCollapsed => CommandResult.Eventcounts.GetCount(HystrixEventType.COLLAPSED);

    public int NumberEmissions => CommandResult.Eventcounts.GetCount(HystrixEventType.EMIT);

    public int NumberFallbackEmissions => CommandResult.Eventcounts.GetCount(HystrixEventType.FALLBACK_EMIT);

    public IHystrixCollapserKey OriginatingCollapserKey => _executionResult.CollapserKey;

    public string PublicCacheKey => CacheKey;

    #endregion IHystrixInvokableInfo

    #region Properties

    protected bool _isFallbackUserDefined;

    public virtual bool IsFallbackUserDefined
    {
        get => _isFallbackUserDefined;

        set => _isFallbackUserDefined = value;
    }

    public Exception ExecutionException => _executionResult.ExecutionException;

    internal ICircuitBreaker CircuitBreaker => _circuitBreaker;

    protected virtual string CacheKey => null;

    protected virtual bool IsRequestCachingEnabled => options.RequestCacheEnabled && CacheKey != null;

    protected virtual string LogMessagePrefix => CommandKey.Name;

    protected virtual ExecutionResult CommandResult
    {
        get
        {
            var resultToReturn = _executionResultAtTimeOfCancellation ?? _executionResult;

            if (_isResponseFromCache)
            {
                resultToReturn = resultToReturn.AddEvent(HystrixEventType.RESPONSE_FROM_CACHE);
            }

            return resultToReturn;
        }
    }

    protected virtual bool ShouldOutputOnNextEvents => false;

    protected virtual bool CommandIsScalar => true;
    #endregion
}
