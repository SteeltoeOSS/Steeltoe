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

using Steeltoe.CircuitBreaker.Hystrix.CircuitBreaker;
using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Strategy;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.EventNotifier;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.ExecutionHook;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Metrics;
using Steeltoe.CircuitBreaker.Hystrix.ThreadPool;
using Steeltoe.CircuitBreaker.Hystrix.Util;

using System;
using System.Collections.Generic;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;
using Microsoft.Extensions.Logging;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public abstract class AbstractCommand<TResult> : IHystrixInvokableInfo, IHystrixInvokable 
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
            public AtomicCommandState(CommandState state) :
                base((int)state)
            {

            }
            public new CommandState Value
            {
                get
                {
                    return (CommandState)_value;
                }
                set
                {
                    _value = (int)value;
                }
            }

            public bool CompareAndSet(CommandState expected, CommandState update)
            {
                return base.CompareAndSet((int)expected, (int)update);
            }
        }

        protected class AtomicThreadState : AtomicInteger
        {
            public AtomicThreadState(ThreadState state) :
                base((int)state)
            {

            }
            public new ThreadState Value
            {
                get
                {
                    return (ThreadState)_value;
                }
                set
                {
                    _value = (int)value;
                }
            }

            public bool CompareAndSet(ThreadState expected, ThreadState update)
            {
                return base.CompareAndSet((int)expected, (int)update);
            }
        }

        protected class AtomicTimedOutStatus : AtomicInteger
        {
            public AtomicTimedOutStatus(TimedOutStatus state) :
                base((int)state)
            {

            }
            public new TimedOutStatus Value
            {
                get
                {
                    return (TimedOutStatus)_value;
                }
                set
                {
                    _value = (int)value;
                }
            }

            public bool CompareAndSet(TimedOutStatus expected, TimedOutStatus update)
            {
                return base.CompareAndSet((int)expected, (int)update);
            }
        }

        protected class HystrixCompletionSource
        {
            private TaskCompletionSource<TResult> source;
            private TResult result;
            private Exception exception;
            private bool? canceled;
            private bool resultSet;

            public HystrixCompletionSource(AbstractCommand<TResult> cmd)
            {
                source = new TaskCompletionSource<TResult>(cmd);
                resultSet = false;
            }

            public TaskCompletionSource<TResult> Source
            {
                get
                {
                    return source;
                }
            }

            public bool IsCanceled
            {
                get
                {
                    return canceled.HasValue ? canceled.Value : false;
                }
            }
            public bool IsCompleted
            {
                get
                {
                    return IsFaulted || IsCanceled || resultSet == true;
                }
            }
            public bool IsFaulted {
                get
                {
                    return exception != null;
                }
            }
            public Exception Exception {
                get
                {
                    return exception;
                }
            }
            public Task<TResult> Task {
                get
                {
                    return source.Task;
                }
            }
            
            public TResult Result {
                get
                {
                    return result;
                }
            }

            internal void TrySetException(Exception exception)
            {
                if (!IsCompleted)
                {
                    this.exception = exception;
                }
            }

            internal void TrySetCanceled()
            {
               if (!IsCompleted)
                {
                    canceled = true; 
                }
             
            }

            internal void TrySetResult(TResult result)
            {
                if (!IsCompleted)
                {
                    resultSet = true;
                    this.result = result;
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
                    source.SetCanceled();
                } else if (IsFaulted)
                {
                    source.SetException(this.exception);
                } else
                {
                    source.SetResult(this.result);
                }
            }
       
        }

        #endregion NestedTypes

        #region Fields
        internal protected long commandStartTimestamp = -1L;
        internal protected long threadStartTimestamp = -1L;
        internal protected volatile bool isResponseFromCache = false;
        protected readonly AtomicCommandState commandState = new AtomicCommandState(CommandState.NOT_STARTED);
        protected readonly AtomicThreadState threadState = new AtomicThreadState(ThreadState.NOT_USING_THREAD);
        protected readonly AtomicTimedOutStatus isCommandTimedOut = new AtomicTimedOutStatus(TimedOutStatus.NOT_EXECUTED);
        internal protected readonly HystrixRequestLog currentRequestLog;
        internal protected readonly HystrixRequestCache requestCache;
        internal protected readonly HystrixCommandExecutionHook executionHook;
        internal protected CancellationTokenSource timeoutTcs;
        internal protected CancellationToken token;
        internal protected CancellationToken usersToken;
        internal protected volatile ExecutionResult executionResult = ExecutionResult.EMPTY; //state on shared execution
        internal protected readonly HystrixCommandMetrics metrics;
        internal protected readonly HystrixEventNotifier eventNotifier;
        internal protected readonly IHystrixCircuitBreaker circuitBreaker;
        internal protected readonly IHystrixThreadPool threadPool;
        internal protected volatile ExecutionResult executionResultAtTimeOfCancellation;
        protected static readonly ConcurrentDictionary<string, SemaphoreSlim> executionSemaphorePerCircuit = new ConcurrentDictionary<string, SemaphoreSlim>();
        protected static readonly ConcurrentDictionary<string, SemaphoreSlim> fallbackSemaphorePerCircuit = new ConcurrentDictionary<string, SemaphoreSlim>();
        internal protected readonly SemaphoreSlim fallbackSemaphoreOverride;
        internal protected readonly SemaphoreSlim executionSemaphoreOverride;
        internal protected readonly HystrixConcurrencyStrategy concurrencyStrategy;
        internal protected Task execThreadTask = null;
        protected HystrixCompletionSource tcs;
        private ILogger logger;

        #endregion Fields

        #region Constructors
        protected AbstractCommand(IHystrixCommandGroupKey group, IHystrixCommandKey key, IHystrixThreadPoolKey threadPoolKey, IHystrixCircuitBreaker circuitBreaker, IHystrixThreadPool threadPool,
                IHystrixCommandOptions commandOptionsDefaults, IHystrixThreadPoolOptions threadPoolOptionsDefaults,
                HystrixCommandMetrics metrics, SemaphoreSlim fallbackSemaphore, SemaphoreSlim executionSemaphore, HystrixOptionsStrategy optionsStrategy, HystrixCommandExecutionHook executionHook, ILogger logger = null)
        {

            this.logger = logger;
            this.commandGroup = InitGroupKey(group);
            this.commandKey = InitCommandKey(key, GetType());
            this.options = InitCommandOptions(this.commandKey, optionsStrategy, commandOptionsDefaults);
            this.threadPoolKey = InitThreadPoolKey(threadPoolKey, this.commandGroup, this.options.ExecutionIsolationThreadPoolKeyOverride);
            this.metrics = InitMetrics(metrics, this.commandGroup, this.threadPoolKey, this.commandKey, this.options);
            this.circuitBreaker = InitCircuitBreaker(this.options.CircuitBreakerEnabled, circuitBreaker, this.commandGroup, this.commandKey, this.options, this.metrics);

            this.threadPool = InitThreadPool(threadPool, this.threadPoolKey, threadPoolOptionsDefaults);
          
            //Strategies from plugins
            this.eventNotifier = HystrixPlugins.EventNotifier;
            this.concurrencyStrategy = HystrixPlugins.ConcurrencyStrategy;
            HystrixMetricsPublisherFactory.CreateOrRetrievePublisherForCommand(this.commandKey, this.commandGroup, this.metrics, this.circuitBreaker, this.options);

            this.executionHook = InitExecutionHook(executionHook);

            this.requestCache = HystrixRequestCache.GetInstance(this.commandKey);
            this.currentRequestLog = InitRequestLog(this.options.RequestLogEnabled);
      
            /* fallback semaphore override if applicable */
            this.fallbackSemaphoreOverride = fallbackSemaphore;

            /* execution semaphore override if applicable */
            this.executionSemaphoreOverride = executionSemaphore;

        }
        #endregion Constructors

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
            if (fromConstructor == null || fromConstructor.Name.Trim().Equals(""))
            {
                string keyName = clazz.Name;
                return HystrixCommandKeyDefault.AsKey(keyName);
            }
            else
            {
                return fromConstructor;
            }
        }

        protected static IHystrixCommandOptions InitCommandOptions(IHystrixCommandKey commandKey, HystrixOptionsStrategy optionsStrategy, IHystrixCommandOptions commandOptionsDefault)
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

        protected static IHystrixThreadPoolKey InitThreadPoolKey(IHystrixThreadPoolKey threadPoolKey, IHystrixCommandGroupKey groupKey, string threadPoolKeyOverride)
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

        protected static HystrixCommandMetrics InitMetrics(HystrixCommandMetrics fromConstructor, IHystrixCommandGroupKey groupKey,
                                                         IHystrixThreadPoolKey threadPoolKey, IHystrixCommandKey commandKey,
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

        protected static IHystrixCircuitBreaker InitCircuitBreaker(bool enabled, IHystrixCircuitBreaker fromConstructor,
                                                                IHystrixCommandGroupKey groupKey, IHystrixCommandKey commandKey,
                                                                IHystrixCommandOptions properties, HystrixCommandMetrics metrics)
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

        protected static IHystrixThreadPool InitThreadPool(IHystrixThreadPool fromConstructor, IHystrixThreadPoolKey threadPoolKey, IHystrixThreadPoolOptions threadPoolPropertiesDefaults)
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
            this.timeoutTcs = CancellationTokenSource.CreateLinkedTokenSource(this.usersToken);
            this.token = timeoutTcs.Token;
            this.tcs = new HystrixCompletionSource(this);

            /* this is a stateful object so can only be used once */
            if (!commandState.CompareAndSet(CommandState.NOT_STARTED, CommandState.OBSERVABLE_CHAIN_CREATED))
            {
                InvalidOperationException ex = new InvalidOperationException("This instance can only be executed once. Please instantiate a new instance.");
                throw new HystrixRuntimeException(FailureType.BAD_REQUEST_EXCEPTION, this.GetType(), LogMessagePrefix + " command executed multiple times - this is not permitted.", ex, null);
            }

            commandStartTimestamp = Time.CurrentTimeMillis;

            if (this.CommandOptions.RequestLogEnabled)
            {
                // log this command execution regardless of what happened
                if (currentRequestLog != null)
                {
                    currentRequestLog.AddExecutedCommand(this);
                }
            }

        }
        protected bool PutInCacheIfAbsent(Task<TResult> hystrixTask, out Task<TResult> fromCache)
        {
            fromCache = null;
            if (IsRequestCachingEnabled && CacheKey != null)
            {
                // wrap it for caching
                fromCache = requestCache.PutIfAbsent<Task<TResult>>(CacheKey, hystrixTask);
                if (fromCache != null)
                {
                    // another thread beat us so we'll use the cached value instead
                    isResponseFromCache = true;
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
                executionHook.OnStart(this);

                /* determine if we're allowed to execute */
                if (circuitBreaker.AllowRequest)
                {
                    SemaphoreSlim executionSemaphore = GetExecutionSemaphore();

                    if (executionSemaphore.TryAcquire())
                    {
                        try
                        {
                            /* used to track userThreadExecutionTime */
                            executionResult = executionResult.SetInvocationStartTime(Time.CurrentTimeMillis);
                            ExecuteCommandWithSpecifiedIsolation();
                            if (tcs.IsFaulted)
                                eventNotifier.MarkEvent(HystrixEventType.EXCEPTION_THROWN, CommandKey);
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

                    if (tcs.IsFaulted) // ???????
                        eventNotifier.MarkEvent(HystrixEventType.EXCEPTION_THROWN, CommandKey);

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
                this.execThreadTask.Start(this.threadPool.GetTaskScheduler());
            } catch(Exception e)
            {
                HandleFallback(e);
                if (tcs.IsFaulted) 
                    eventNotifier.MarkEvent(HystrixEventType.EXCEPTION_THROWN, CommandKey);

                UnsubscribeCommandCleanup();
                FireOnCompletedHook();
                TerminateCommandCleanup();

            }

        }
        protected Exception DecomposeException(Exception e)
        {

            if (e is HystrixBadRequestException)
            {
                return (HystrixBadRequestException)e;
            }
            if (e.InnerException is HystrixBadRequestException)
            {
                return (HystrixBadRequestException)e.InnerException;
            }
            if (e is HystrixRuntimeException)
            {
                return (HystrixRuntimeException)e;
            }
            // if we have an exception we know about we'll throw it directly without the wrapper exception
            if (e.InnerException is HystrixRuntimeException)
            {
                return (HystrixRuntimeException)e.InnerException;
            }
            // we don't know what kind of exception this is so create a generic message and throw a new HystrixRuntimeException
            String message = LogMessagePrefix + " failed while executing. {0}";
            logger?.LogDebug(message, e); // debug only since we're throwing the exception and someone higher will do something with it
            return new HystrixRuntimeException(FailureType.COMMAND_EXCEPTION, this.GetType(), message, e, null);
        }
        private void TimeoutThreadAction()
        {

            if (!Time.WaitUntil(() =>
            {
                return isCommandTimedOut.Value == TimedOutStatus.COMPLETED;

            }, options.ExecutionTimeoutInMilliseconds))
            {
                if (this.isCommandTimedOut.CompareAndSet(TimedOutStatus.NOT_EXECUTED, TimedOutStatus.TIMED_OUT))
                {
                    timeoutTcs.Cancel();

                    // report timeout failure
                    this.eventNotifier.MarkEvent(HystrixEventType.TIMEOUT, this.commandKey);

                    if (threadState.CompareAndSet(ThreadState.STARTED, ThreadState.UNSUBSCRIBED))
                    {
                        HandleThreadEnd();
                    }
                    threadState.CompareAndSet(ThreadState.NOT_USING_THREAD, ThreadState.UNSUBSCRIBED);

                    HandleFallback(new HystrixTimeoutException("timed out while executing run()"));

                    if (tcs.IsFaulted) // ???????
                        eventNotifier.MarkEvent(HystrixEventType.EXCEPTION_THROWN, CommandKey);

                    UnsubscribeCommandCleanup();
                    FireOnCompletedHook();
                    TerminateCommandCleanup();
                }
            }
        }
        private void ExecuteCommandWithThreadAction()
        {
            this.threadStartTimestamp = Time.CurrentTimeMillis;

            if (this.token.IsCancellationRequested)
            {
                tcs.TrySetCanceled();
                UnsubscribeCommandCleanup();
                threadState.CompareAndSet(ThreadState.NOT_USING_THREAD, ThreadState.UNSUBSCRIBED);
                TerminateCommandCleanup();
                return;
            }

            try
            {
                executionResult = executionResult.SetExecutionOccurred();
                if (!commandState.CompareAndSet(CommandState.OBSERVABLE_CHAIN_CREATED, CommandState.USER_CODE_EXECUTED))
                {
                    tcs.TrySetException(new InvalidOperationException("execution attempted while in state : " + commandState.Value));
                    return;
                }

                metrics.MarkCommandStart(commandKey, threadPoolKey, ExecutionIsolationStrategy.THREAD);

                if (isCommandTimedOut.Value == TimedOutStatus.TIMED_OUT)
                {
                    // the command timed out in the wrapping thread so we will return immediately
                    // and not increment any of the counters below or other such logic
                    tcs.TrySetException(new HystrixTimeoutException("timed out before executing run()"));
                    return;
                }
                if (threadState.CompareAndSet(ThreadState.NOT_USING_THREAD, ThreadState.STARTED))
                {
                    //we have not been unsubscribed, so should proceed
                    HystrixCounters.IncrementGlobalConcurrentThreads();
                    threadPool.MarkThreadExecution();
                    // store the command that is being run
                    //endCurrentThreadExecutingCommand = Hystrix.StartCurrentThreadExecutingCommand(CommandKey);
                    executionResult = executionResult.SetExecutedInThread();
                    /**
                     * If any of these hooks throw an exception, then it appears as if the actual execution threw an error
                     */
                    try
                    {

                        if (options.ExecutionTimeoutEnabled)
                        {

                            Task timerTask = new Task(() => { TimeoutThreadAction(); }, TaskCreationOptions.LongRunning);
                            timerTask.Start(TaskScheduler.Default);
                        }

                        executionHook.OnThreadStart(this);
                        //executionHook.OnRunStart(_cmd);  Deprecated
                        executionHook.OnExecutionStart(this);
                        TResult result = ExecuteRun();
                        if (isCommandTimedOut.Value != TimedOutStatus.TIMED_OUT)
                        {
                            MarkEmits();
                            result = (TResult)WrapWithOnEmitHook(result);
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
                    //command has already been unsubscribed, so return immediately
                    tcs.TrySetException(new Exception("unsubscribed before executing run()"));
                    return;
                }
            }
            finally
            {
                if (isCommandTimedOut.Value != TimedOutStatus.TIMED_OUT)
                {
                    // applyHystrixSemantics.doOnError(markexceptionthrown)
                    if (tcs.IsFaulted) // ???????
                        eventNotifier.MarkEvent(HystrixEventType.EXCEPTION_THROWN, CommandKey);

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
                Action threadExec = () =>  { ExecuteCommandWithThreadAction();  };
                execThreadTask = new Task(threadExec, CancellationToken.None, TaskCreationOptions.LongRunning);

            }
            else
            {

                executionResult = executionResult.SetExecutionOccurred();
                if (!commandState.CompareAndSet(CommandState.OBSERVABLE_CHAIN_CREATED, CommandState.USER_CODE_EXECUTED))
                {
                    throw new InvalidOperationException("execution attempted while in state : " + commandState.Value);
                }

                metrics.MarkCommandStart(commandKey, threadPoolKey, ExecutionIsolationStrategy.SEMAPHORE);

                if (options.ExecutionTimeoutEnabled)
                {
                    Task timerTask = new Task(() => { TimeoutThreadAction(); }, TaskCreationOptions.LongRunning);
                    timerTask.Start(TaskScheduler.Default);
                }

                try
                {
                    executionHook.OnExecutionStart(this);
                    TResult result = ExecuteRun();  //the getUserExecutionObservable method already wraps sync exceptions, so this shouldn't throw
                    if (isCommandTimedOut.Value != TimedOutStatus.TIMED_OUT)
                    {
                        MarkEmits();
                        result = WrapWithOnEmitHook(result);
                        tcs.TrySetResult(result);
                        WrapWithOnExecutionSuccess();
                        MarkCompleted();

                    }
                } catch (Exception e)
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
               
                TResult result = DoRun();
                isCommandTimedOut.CompareAndSet(TimedOutStatus.NOT_EXECUTED, TimedOutStatus.COMPLETED);
    
                if (isCommandTimedOut.Value != TimedOutStatus.TIMED_OUT)
                    result = WrapWithOnExecutionEmitHook(result);
                else
                    result = default(TResult);
                return result;

            }
            catch (AggregateException e)
            {

                isCommandTimedOut.CompareAndSet(TimedOutStatus.NOT_EXECUTED, TimedOutStatus.COMPLETED);

                Exception flatten = GetException(e);
                if (flatten.InnerException is TaskCanceledException && isCommandTimedOut.Value == TimedOutStatus.TIMED_OUT)
                {
                    // End task pass
                    return default(TResult);
                }


                Exception ex = WrapWithOnExecutionErrorHook(flatten.InnerException);
                if (ex == flatten.InnerException)
                    throw;

                throw ex;
            } catch (OperationCanceledException e)
            {
                isCommandTimedOut.CompareAndSet(TimedOutStatus.NOT_EXECUTED, TimedOutStatus.COMPLETED);

                if (isCommandTimedOut.Value == TimedOutStatus.TIMED_OUT)
                {
                    // End task pass
                    return default(TResult);
                }

                Exception ex = WrapWithOnExecutionErrorHook(e);
                if (e == ex)
                    throw;

                throw ex;
           
            }
            catch (Exception ex)
            {
                isCommandTimedOut.CompareAndSet(TimedOutStatus.NOT_EXECUTED, TimedOutStatus.COMPLETED);

                //if (options.ExecutionTimeoutEnabled)
                //{

                //    this.token = this.usersToken;
                //}

                Exception returned = WrapWithOnExecutionErrorHook(ex);
                if (ex == returned)
                    throw;
                throw returned;
            }
        }
        private TResult ExecuteRunFallback()
        {
       
            return DoFallback();

        }
        private bool IsUnrecoverableError(Exception t)
        {
  
            Exception cause = t;

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
                    if (!executionResult.ContainsTerminalEvent)
                    {
                        eventNotifier.MarkEvent(HystrixEventType.CANCELLED, CommandKey);
                        executionResultAtTimeOfCancellation = executionResult
                                .AddEvent((int)(Time.CurrentTimeMillis - commandStartTimestamp), HystrixEventType.CANCELLED);
                    }
                    HandleCommandEnd(false); //user code never ran
                }
                else if (commandState.CompareAndSet(CommandState.USER_CODE_EXECUTED, CommandState.UNSUBSCRIBED))
                {
                    if (!executionResult.ContainsTerminalEvent)
                    {
                        eventNotifier.MarkEvent(HystrixEventType.CANCELLED, CommandKey);
                        executionResultAtTimeOfCancellation = executionResult
                                .AddEvent((int)(Time.CurrentTimeMillis - commandStartTimestamp), HystrixEventType.CANCELLED);
                    }
                    HandleCommandEnd(true); //user code did run
                }
            }
        }
        private void TerminateCommandCleanup()
        {
            if (tcs.IsCompleted)
            {
                if (commandState.CompareAndSet(CommandState.OBSERVABLE_CHAIN_CREATED, CommandState.TERMINAL))
                {
                    HandleCommandEnd(false); //user code never ran
                }
                else if (commandState.CompareAndSet(CommandState.USER_CODE_EXECUTED, CommandState.TERMINAL))
                {
                    HandleCommandEnd(true); //user code did run
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
                    executionHook.OnSuccess(this);
                }
                catch (Exception hookEx)
                {
                    logger?.LogWarning("Error calling HystrixCommandExecutionHook.onSuccess - {0}", hookEx);
                }
            }
        }
        private Exception GetException(AggregateException e)
        {
            return e.Flatten();
        }
        protected internal SemaphoreSlim GetExecutionSemaphore()
        {
            if (options.ExecutionIsolationStrategy == ExecutionIsolationStrategy.SEMAPHORE)
            {
                if (executionSemaphoreOverride == null)
                {
                    return executionSemaphorePerCircuit.GetOrAddEx(commandKey.Name, (k) => new SemaphoreSlim(options.ExecutionIsolationSemaphoreMaxConcurrentRequests));
                }
                else
                {
                    return executionSemaphoreOverride;
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
            if (fallbackSemaphoreOverride == null)
            {
                return fallbackSemaphorePerCircuit.GetOrAddEx(commandKey.Name, (k) => new SemaphoreSlim(options.FallbackIsolationSemaphoreMaxConcurrentRequests));
            }
            else
            {
                return fallbackSemaphoreOverride;
            }
        }
        protected abstract TResult DoRun();
        protected abstract TResult DoFallback();

        #region Marks
        private void MarkFallbackEmit()
        {
            if (ShouldOutputOnNextEvents)
            {
                executionResult = executionResult.AddEvent(HystrixEventType.FALLBACK_EMIT);
                eventNotifier.MarkEvent(HystrixEventType.FALLBACK_EMIT, commandKey);
            }
        }
        private void MarkFallbackCompleted() 
        {

            long latency2 = Time.CurrentTimeMillis - executionResult.StartTimestamp;
            eventNotifier.MarkEvent(HystrixEventType.FALLBACK_SUCCESS, commandKey);
            executionResult = executionResult.AddEvent((int)latency2, HystrixEventType.FALLBACK_SUCCESS);

        }
        private void MarkEmits()
        {
            if (ShouldOutputOnNextEvents)
            {
                executionResult = executionResult.AddEvent(HystrixEventType.EMIT);
                eventNotifier.MarkEvent(HystrixEventType.EMIT, commandKey);
            }
            if (CommandIsScalar)
            {
                long latency = Time.CurrentTimeMillis - executionResult.StartTimestamp;
                eventNotifier.MarkCommandExecution(CommandKey, options.ExecutionIsolationStrategy, (int)latency, executionResult.OrderedList);
                eventNotifier.MarkEvent(HystrixEventType.SUCCESS, commandKey);
                executionResult = executionResult.AddEvent((int)latency, HystrixEventType.SUCCESS);
                circuitBreaker.MarkSuccess();
            }
        }
        private void MarkCompleted()
        {
            if (tcs.IsCompleted)
            {
                if (!CommandIsScalar)
                {
                    long latency = Time.CurrentTimeMillis - executionResult.StartTimestamp;
                    eventNotifier.MarkCommandExecution(CommandKey, options.ExecutionIsolationStrategy, (int)latency, executionResult.OrderedList);
                    eventNotifier.MarkEvent(HystrixEventType.SUCCESS, commandKey);
                    executionResult = executionResult.AddEvent((int)latency, HystrixEventType.SUCCESS);
                    circuitBreaker.MarkSuccess();
                }
            }
        }
        internal void MarkAsCollapsedCommand(IHystrixCollapserKey collapserKey, int sizeOfBatch)
        {
            eventNotifier.MarkEvent(HystrixEventType.COLLAPSED, this.commandKey);
            executionResult = executionResult.MarkCollapsed(collapserKey, sizeOfBatch);
        }
        #endregion Marks
        
        #region Wraps
        private void WrapWithOnFallbackSuccessHook()
        {
            try
            {
                executionHook.OnFallbackSuccess(this);
            }
            catch (Exception hookEx)
            {
                logger?.LogWarning("Error calling HystrixCommandExecutionHook.onFallbackSuccess - {0}", hookEx);
            }
        }
        private TResult WrapWithOnFallbackEmitHook(TResult r)
        {
            try
            {
                return executionHook.OnFallbackEmit(this, r);
            }
            catch (Exception hookEx )
            {
                logger?.LogWarning("Error calling HystrixCommandExecutionHook.onFallbackEmit - {0}", hookEx);
                return r;
            }
        }
        private void WrapWithOnFallbackStartHook()
        {
            try
            {
                executionHook.OnFallbackStart(this);
            }
            catch (Exception hookEx )
            {
                logger?.LogWarning("Error calling HystrixCommandExecutionHook.OnFallbackStart - {0}", hookEx);
            }
        }
        private TResult WrapWithOnEmitHook(TResult result)
        {

            try
            {
                return executionHook.OnEmit(this, result);
            }
            catch (Exception hookEx )
            {
                logger?.LogWarning("Error calling HystrixCommandExecutionHook.onEmit - {0}", hookEx);
                return result;
            }


        }
        private Exception WrapWithOnErrorHook(FailureType failureType, Exception e)
        {
            try
            {
                return executionHook.OnError(this, failureType, e);
            }
            catch (Exception hookEx )
            {
                logger?.LogWarning("Error calling HystrixCommandExecutionHook.onError - {0}", hookEx);
                return e;
            }
        }
        private void WrapWithOnExecutionSuccess()
        {
            try
            {
                executionHook.OnExecutionSuccess(this);
            }
            catch (Exception hookEx )
            {
                logger?.LogWarning("Error calling HystrixCommandExecutionHook.onExecutionSuccess - {0}", hookEx);
            }
        }
        private Exception WrapWithOnExecutionErrorHook(Exception e)
        {
            try
            {
                return executionHook.OnExecutionError(this, e);
            }
            catch (Exception hookEx )
            {
                logger?.LogWarning("Error calling HystrixCommandExecutionHook.onExecutionError - {0}", hookEx);
                return e;
            }
        }
        private TResult WrapWithOnExecutionEmitHook(TResult r)
        {
            try
            {
                return executionHook.OnExecutionEmit(this, r);
            } catch (Exception hookEx )
            {
                logger?.LogWarning("Error calling HystrixCommandExecutionHook.onExecutionEmit - {0}", hookEx);
                return r;
            }
        }
        private Exception WrapWithOnFallbackErrorHook(Exception e)
        {
            try
            {
                if (IsFallbackUserDefined)
                {
                    return executionHook.OnFallbackError(this, e);
                }
                else
                {
                    return e;
                }
            }
            catch (Exception hookEx)
            {
                logger?.LogWarning("Error calling HystrixCommandExecutionHook.onFallbackError - {0}", hookEx);
                return e;
            }
        }
        #endregion Wraps

        #region Handlers
        private void HandleFallbackOrThrowException(HystrixEventType eventType, FailureType failureType, string message, Exception originalException)
        {
            long latency = Time.CurrentTimeMillis - executionResult.StartTimestamp;
            // record the executionResult
            // do this before executing fallback so it can be queried from within getFallback (see See https://github.com/Netflix/Hystrix/pull/144)
            executionResult = executionResult.AddEvent((int)latency, eventType);

            if (IsUnrecoverableError(originalException))
            {
                Exception e = originalException;
                logger?.LogError("Unrecoverable Error for HystrixCommand so will throw HystrixRuntimeException and not apply fallback: {0} ", e);

                /* executionHook for all errors */
                e = WrapWithOnErrorHook(failureType, e);
                tcs.TrySetException(
                    new HystrixRuntimeException(failureType, this.GetType(), LogMessagePrefix + " " + message + " and encountered unrecoverable error.", e, null));
                return;
            }
            else
            {
                if (IsRecoverableError(originalException))
                {
                    logger?.LogWarning("Recovered from Error by serving Hystrix fallback: {0}", originalException);
                }

                if (options.FallbackEnabled)
                {
                    /* fallback behavior is permitted so attempt */

                    SemaphoreSlim fallbackSemaphore = GetFallbackSemaphore();


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
                                //same logic as above without the hook invocation
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
                            return;

                        }
                        finally
                        {
                            fallbackSemaphore?.Release();
                        }

                        tcs.TrySetResult((TResult)fallbackExecutionResult);
                        return;

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
            Exception e = originalException;

            if (fe is InvalidOperationException)
            {
                long latency = Time.CurrentTimeMillis - executionResult.StartTimestamp;
                logger?.LogDebug("No fallback for HystrixCommand: {0} ", fe); // debug only since we're throwing the exception and someone higher will do something with it
                eventNotifier.MarkEvent(HystrixEventType.FALLBACK_MISSING, commandKey);
                executionResult = executionResult.AddEvent((int)latency, HystrixEventType.FALLBACK_MISSING);

                /* executionHook for all errors */
                e = WrapWithOnErrorHook(failureType, e);

                tcs.TrySetException(new HystrixRuntimeException(failureType, this.GetType(), LogMessagePrefix + " " + message + " and no fallback available.", e, fe));
            }
            else
            {
                long latency = Time.CurrentTimeMillis - executionResult.StartTimestamp;
                logger?.LogDebug("HystrixCommand execution {0} and fallback failed: {1}", failureType.ToString(), fe);
                eventNotifier.MarkEvent(HystrixEventType.FALLBACK_FAILURE, commandKey);
                executionResult = executionResult.AddEvent((int)latency, HystrixEventType.FALLBACK_FAILURE);

                /* executionHook for all errors */
                e = WrapWithOnErrorHook(failureType, e);

                tcs.TrySetException(new HystrixRuntimeException(failureType, this.GetType(), LogMessagePrefix + " " + message + " and fallback failed.", e, fe));
            }

        }
        private void HandleFallbackDisabledByEmittingError(Exception underlying, FailureType failureType, String message)
        {
            /* fallback is disabled so throw HystrixRuntimeException */
            logger?.LogDebug("Fallback disabled for HystrixCommand so will throw HystrixRuntimeException: {0} ", underlying); // debug only since we're throwing the exception and someone higher will do something with it

            /* executionHook for all errors */
            Exception wrapped = WrapWithOnErrorHook(failureType, underlying);
            tcs.TrySetException(new HystrixRuntimeException(failureType, this.GetType(), LogMessagePrefix + " " + message + " and fallback disabled.", wrapped, null));
        }
        private void HandleFallbackRejectionByEmittingError()
        {
            long latencyWithFallback = Time.CurrentTimeMillis - executionResult.StartTimestamp;
            eventNotifier.MarkEvent(HystrixEventType.FALLBACK_REJECTION, commandKey);
            executionResult = executionResult.AddEvent((int)latencyWithFallback, HystrixEventType.FALLBACK_REJECTION);
            logger?.LogDebug("HystrixCommand Fallback Rejection."); // debug only since we're throwing the exception and someone higher will do something with it
            // if we couldn't acquire a permit, we "fail fast" by throwing an exception
            tcs.TrySetException(new HystrixRuntimeException(FailureType.REJECTED_SEMAPHORE_FALLBACK, this.GetType(), LogMessagePrefix + " fallback execution rejected.", null, null));
        }
        private void HandleSemaphoreRejectionViaFallback()
        {
            Exception semaphoreRejectionException = new Exception("could not acquire a semaphore for execution");
            executionResult = executionResult.SetExecutionException(semaphoreRejectionException);
            eventNotifier.MarkEvent(HystrixEventType.SEMAPHORE_REJECTED, commandKey);
            logger?.LogDebug("HystrixCommand Execution Rejection by Semaphore."); // debug only since we're throwing the exception and someone higher will do something with it
            // retrieve a fallback or throw an exception if no fallback available
            HandleFallbackOrThrowException(HystrixEventType.SEMAPHORE_REJECTED, FailureType.REJECTED_SEMAPHORE_EXECUTION,
                    "could not acquire a semaphore for execution", semaphoreRejectionException);
        }
        private void HandleShortCircuitViaFallback()
        {
            // record that we are returning a short-circuited fallback
            eventNotifier.MarkEvent(HystrixEventType.SHORT_CIRCUITED, commandKey);
            // short-circuit and go directly to fallback (or throw an exception if no fallback implemented)
            Exception shortCircuitException = new Exception("Hystrix circuit short-circuited and is OPEN");
            executionResult = executionResult.SetExecutionException(shortCircuitException);
            try
            {
                HandleFallbackOrThrowException(HystrixEventType.SHORT_CIRCUITED, FailureType.SHORTCIRCUIT, "short-circuited", shortCircuitException);
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
            executionResult = executionResult.SetExecutionException(e);
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

            logger?.LogDebug("Error executing HystrixCommand.Run(). Proceeding to fallback logic...: {0}", underlying);

            // report failure
            eventNotifier.MarkEvent(HystrixEventType.FAILURE, commandKey);

            // record the exception
            executionResult = executionResult.SetException(underlying);
            HandleFallbackOrThrowException(HystrixEventType.FAILURE, FailureType.COMMAND_EXCEPTION, "failed", underlying);
        }
        private void HandleBadRequestByEmittingError(Exception underlying)
        {
            Exception toEmit = underlying;

            try
            {
                long executionLatency = Time.CurrentTimeMillis - executionResult.StartTimestamp;
                eventNotifier.MarkEvent(HystrixEventType.BAD_REQUEST, commandKey);
                executionResult = executionResult.AddEvent((int)executionLatency, HystrixEventType.BAD_REQUEST);
                Exception decorated = executionHook.OnError(this, FailureType.BAD_REQUEST_EXCEPTION, underlying);

                if (decorated is HystrixBadRequestException)
                {
                    toEmit = decorated;
                }
                else
                {
                    logger?.LogWarning("ExecutionHook.onError returned an exception that was not an instance of HystrixBadRequestException so will be ignored: {0}", decorated);
                }
            }
            catch (Exception hookEx)
            {
                logger?.LogWarning("Error calling HystrixCommandExecutionHook.onError: {0}", hookEx);
            }

            tcs.TrySetException(toEmit);
        }
        private void HandleTimeoutViaFallback()
        {
            HandleFallbackOrThrowException(HystrixEventType.TIMEOUT, FailureType.TIMEOUT, "timed-out", new TimeoutException());
        }
        private void HandleThreadPoolRejectionViaFallback(Exception underlying)
        {
            eventNotifier.MarkEvent(HystrixEventType.THREAD_POOL_REJECTED, commandKey);
            threadPool.MarkThreadRejection();
            // use a fallback instead (or throw exception if not implemented)
            HandleFallbackOrThrowException(HystrixEventType.THREAD_POOL_REJECTED, FailureType.REJECTED_THREAD_EXECUTION, "could not be queued for execution", underlying);
        }
        private void HandleRequestCacheHitAndEmitValues(Task fromCache, AbstractCommand<TResult> _cmd)
        {
            try
            {
                executionHook.OnCacheHit(this);
            }
            catch (Exception hookEx)
            {
                logger?.LogWarning("Error calling HystrixCommandExecutionHook.onCacheHit: {0}", hookEx);
            }

            if (_cmd.token.IsCancellationRequested)
            {
                _cmd.executionResult = _cmd.executionResult.AddEvent(HystrixEventType.CANCELLED);
                _cmd.executionResult = _cmd.executionResult.SetExecutionLatency(-1);
            }
            else
            {
                if (!fromCache.IsCompleted)
                {
                    fromCache.Wait(_cmd.token);
                }
                AbstractCommand<TResult> originalCommand = fromCache.AsyncState as AbstractCommand<TResult>;
                if (originalCommand != null)
                {
                    _cmd.executionResult = originalCommand.executionResult;
                }
            }

            if (_cmd.token.IsCancellationRequested)
            {
                if (commandState.CompareAndSet(CommandState.OBSERVABLE_CHAIN_CREATED, CommandState.UNSUBSCRIBED))
                {
                    HandleCleanUpAfterResponseFromCache(false); //user code never ran
                }
                else if (commandState.CompareAndSet(CommandState.USER_CODE_EXECUTED, CommandState.UNSUBSCRIBED))
                {
                    HandleCleanUpAfterResponseFromCache(true); //user code did run
                }
            }

            if (fromCache.IsCompleted)
            {
                if (commandState.CompareAndSet(CommandState.OBSERVABLE_CHAIN_CREATED, CommandState.TERMINAL))
                {
                    HandleCleanUpAfterResponseFromCache(false); //user code never ran
                }
                else if (commandState.CompareAndSet(CommandState.USER_CODE_EXECUTED, CommandState.TERMINAL))
                {
                    HandleCleanUpAfterResponseFromCache(true); //user code did run
                }
            }
        }
        protected virtual void HandleCleanUpAfterResponseFromCache(bool commandExecutionStarted)
        {

            long latency = Time.CurrentTimeMillis - commandStartTimestamp;
            executionResult = executionResult.AddEvent(-1, HystrixEventType.RESPONSE_FROM_CACHE)
                    .MarkUserThreadCompletion(latency)
                    .SetNotExecutedInThread();
            ExecutionResult cacheOnlyForMetrics = ExecutionResult.From(HystrixEventType.RESPONSE_FROM_CACHE)
                    .MarkUserThreadCompletion(latency);
            metrics.MarkCommandDone(cacheOnlyForMetrics, commandKey, threadPoolKey, commandExecutionStarted);
            eventNotifier.MarkEvent(HystrixEventType.RESPONSE_FROM_CACHE, commandKey);
        }
        protected virtual void HandleCommandEnd(bool commandExecutionStarted)
        {

            long userThreadLatency = Time.CurrentTimeMillis - commandStartTimestamp;
            executionResult = executionResult.MarkUserThreadCompletion((int)userThreadLatency);
            if (executionResultAtTimeOfCancellation == null)
            {
                metrics.MarkCommandDone(executionResult, commandKey, threadPoolKey, commandExecutionStarted);
            }
            else
            {
                metrics.MarkCommandDone(executionResultAtTimeOfCancellation, commandKey, threadPoolKey, commandExecutionStarted);
            }

        }
        protected virtual void HandleThreadEnd()
        {
            HystrixCounters.DecrementGlobalConcurrentThreads();
            threadPool.MarkThreadCompletion();
            try
            {
                executionHook.OnThreadComplete(this);
            }
            catch (Exception hookEx)
            {
                logger?.LogWarning("Error calling HystrixCommandExecutionHook.onThreadComplete: {0}", hookEx);
            }
        }
        #endregion Handlers

        #region IHystrixInvokableInfo
        protected readonly IHystrixCommandGroupKey commandGroup;
        public IHystrixCommandGroupKey CommandGroup
        {
            get
            {
                return commandGroup;
            }
        }
        protected readonly IHystrixCommandKey commandKey;
        public IHystrixCommandKey CommandKey
        {
            get
            {
                return commandKey;
            }
        }
        protected readonly IHystrixThreadPoolKey threadPoolKey;
        public IHystrixThreadPoolKey ThreadPoolKey
        {
            get
            {
                return threadPoolKey;
            }
        }
        protected readonly IHystrixCommandOptions options;
        public IHystrixCommandOptions CommandOptions
        {
            get
            {
                return options;
            }
        }

        public long CommandRunStartTimeInNanos
        {
            get
            {
                return executionResult.CommandRunStartTimeInNanos;
            }
        }

        public ExecutionResult.EventCounts EventCounts
        {
            get
            {
                return CommandResult.Eventcounts;
            }
        }

        public List<HystrixEventType> ExecutionEvents
        {
            get
            {
                return CommandResult.OrderedList;
            }
        }

        public int ExecutionTimeInMilliseconds
        {
            get
            {
                return CommandResult.ExecutionLatency;
            }
        }

        public Exception FailedExecutionException
        {
            get
            {
                return executionResult.Exception;
            }
        }

        public bool IsCircuitBreakerOpen
        {
            get
            {
                return options.CircuitBreakerForceOpen || (!options.CircuitBreakerForceClosed && circuitBreaker.IsOpen);
            }
        }

        public bool IsExecutedInThread
        {
            get
            {
                return CommandResult.IsExecutedInThread;
            }
        }

        public bool IsExecutionComplete
        {
            get
            {
                return commandState.Value == CommandState.TERMINAL;
            }
        }

        public bool IsFailedExecution
        {
            get
            {
                return CommandResult.Eventcounts.Contains(HystrixEventType.FAILURE);
            }
        }

        public bool IsResponseFromCache
        {
            get
            {
                return isResponseFromCache;
            }
        }

        public bool IsResponseFromFallback
        {
            get
            {
                return CommandResult.Eventcounts.Contains(HystrixEventType.FALLBACK_SUCCESS);
            }
        }

        public bool IsResponseRejected
        {
            get
            {
                return CommandResult.IsResponseRejected;
            }
        }

        public bool IsResponseSemaphoreRejected
        {
            get
            {
                return CommandResult.IsResponseSemaphoreRejected;
            }
        }

        public bool IsResponseShortCircuited
        {
            get
            {
                return CommandResult.Eventcounts.Contains(HystrixEventType.SHORT_CIRCUITED);
            }
        }

        public bool IsResponseThreadPoolRejected
        {
            get
            {
                return CommandResult.IsResponseThreadPoolRejected;
            }
        }

        public bool IsResponseTimedOut
        {
            get
            {
                return CommandResult.Eventcounts.Contains(HystrixEventType.TIMEOUT);
            }
        }

        public bool IsSuccessfulExecution
        {
            get
            {
                return CommandResult.Eventcounts.Contains(HystrixEventType.SUCCESS);
            }
        }

        public HystrixCommandMetrics Metrics
        {
            get
            {
                return metrics;
            }
        }

        public int NumberCollapsed
        {
            get
            {
                return CommandResult.Eventcounts.GetCount(HystrixEventType.COLLAPSED);
            }
        }

        public int NumberEmissions
        {
            get
            {
                return CommandResult.Eventcounts.GetCount(HystrixEventType.EMIT);
            }
        }

        public int NumberFallbackEmissions
        {
            get
            {
                return CommandResult.Eventcounts.GetCount(HystrixEventType.FALLBACK_EMIT);
            }
        }

        public IHystrixCollapserKey OriginatingCollapserKey
        {
            get
            {
                return executionResult.CollapserKey;
            }
        }

        public string PublicCacheKey
        {
            get
            {
                return CacheKey;
            }
        }

        #endregion IHystrixInvokableInfo

        #region Properties

        protected bool _isFallbackUserDefined = false;
        public virtual bool IsFallbackUserDefined
        {
            get
            {
                return _isFallbackUserDefined;
            }
            set
            {
                _isFallbackUserDefined = value;
            }
        }
        public Exception ExecutionException
        {
            get { return executionResult.ExecutionException; }
        }

        protected virtual string CacheKey
        {
            get { return null; }
        }

        protected virtual bool IsRequestCachingEnabled
        {
            get { return options.RequestCacheEnabled && CacheKey != null; }
        }
        protected virtual string LogMessagePrefix
        {
            get
            {
                return CommandKey.Name;
            }
        }
        protected virtual ExecutionResult CommandResult
        {
            get
            {
                ExecutionResult resultToReturn;
                if (executionResultAtTimeOfCancellation == null)
                {
                    resultToReturn = executionResult;
                }
                else
                {
                    resultToReturn = executionResultAtTimeOfCancellation;
                }

                if (isResponseFromCache)
                {
                    resultToReturn = resultToReturn.AddEvent(HystrixEventType.RESPONSE_FROM_CACHE);
                }

                return resultToReturn;
            }
        }

        protected virtual bool ShouldOutputOnNextEvents
        {
            get { return false; }
        }
        protected virtual bool CommandIsScalar
        {
            get { return true; }
        }

        internal IHystrixCircuitBreaker CircuitBreaker
        {
            get { return this.circuitBreaker;  }
        }
        #endregion
    }
}
