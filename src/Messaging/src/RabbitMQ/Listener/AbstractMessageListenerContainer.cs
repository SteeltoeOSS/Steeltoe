// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Transaction;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Batch;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Listener.Support;
using Steeltoe.Messaging.RabbitMQ.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using R = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

public abstract class AbstractMessageListenerContainer : IMessageListenerContainer
{
    public const int DEFAULT_FAILED_DECLARATION_RETRY_INTERVAL = 5000;
    public const long DEFAULT_SHUTDOWN_TIMEOUT = 5000;
    public const int DEFAULT_RECOVERY_INTERVAL = 5000;
    public const bool DEFAULT_DEBATCHING_ENABLED = true;
    public const int DEFAULT_PREFETCH_COUNT = 250;

    protected readonly object _consumersMonitor = new ();
    protected readonly object _lock = new ();
    protected readonly object _lifecycleMonitor = new ();
    protected readonly ILogger _logger;
    protected readonly ILoggerFactory _loggerFactory;

    protected int _recoveryInterval = DEFAULT_RECOVERY_INTERVAL;
    private string _listenerid;
    private IConnectionFactory _connectionFactory;

    private List<IQueue> Queues { get; set; } = new ();

    protected AbstractMessageListenerContainer(IApplicationContext applicationContext, IConnectionFactory connectionFactory, string name = null, ILoggerFactory loggerFactory = null)
    {
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory?.CreateLogger(GetType());
        ApplicationContext = applicationContext;
        ConnectionFactory = connectionFactory;
        ErrorHandler = new ConditionalRejectingErrorHandler(_logger);
        MessageHeadersConverter = new DefaultMessageHeadersConverter(_logger);
        ExclusiveConsumerExceptionLogger = new DefaultExclusiveConsumerLogger();
        BatchingStrategy = new SimpleBatchingStrategy(0, 0, 0L);
        TransactionAttribute = new DefaultTransactionAttribute();
        ServiceName = name ?? $"{GetType().Name}@{GetHashCode()}";
    }

    public virtual IConnectionFactory ConnectionFactory
    {
        get
        {
            _connectionFactory ??= ApplicationContext.GetService<IConnectionFactory>();
            return _connectionFactory;
        }

        set
        {
            _connectionFactory = value;
        }
    }

    public virtual bool IsChannelTransacted { get; set; }

    public IApplicationContext ApplicationContext { get; set; }

    public virtual AcknowledgeMode AcknowledgeMode { get; set; } = AcknowledgeMode.AUTO;

    public virtual string ServiceName { get; set; }

    public virtual bool ExposeListenerChannel { get; set; } = true;

    public virtual IMessageListener MessageListener { get; set; }

    public virtual IErrorHandler ErrorHandler { get; set; }

    public virtual bool IsDeBatchingEnabled { get; set; } = DEFAULT_DEBATCHING_ENABLED;

    public virtual IList<IMessagePostProcessor> AfterReceivePostProcessors { get; private set; }

    public virtual bool IsAutoStartup { get; set; } = true;

    public virtual int Phase { get; set; } = int.MaxValue;

    public virtual string LookupKeyQualifier { get; set; } = string.Empty;

    public virtual string ListenerId
    {
        get => _listenerid ?? ServiceName;

        set => _listenerid = value;
    }

    public virtual IConsumerTagStrategy ConsumerTagStrategy { get; set; }

    public virtual Dictionary<string, object> ConsumerArguments { get; set; } = new ();

    public virtual bool Exclusive { get; set; }

    public virtual bool NoLocal { get; set; }

    public virtual bool DefaultRequeueRejected { get; set; } = true;

    public virtual int PrefetchCount { get; set; } = DEFAULT_PREFETCH_COUNT;

    public virtual long ShutdownTimeout { get; set; } = DEFAULT_SHUTDOWN_TIMEOUT;

    public virtual long IdleEventInterval { get; set; }

    public virtual int RecoveryInterval
    {
        get => _recoveryInterval;
        set
        {
            _recoveryInterval = value;
            RecoveryBackOff = new FixedBackOff(_recoveryInterval, FixedBackOff.UNLIMITED_ATTEMPTS);
        }
    }

    public IBackOff RecoveryBackOff { get; set; } = new FixedBackOff(DEFAULT_RECOVERY_INTERVAL, FixedBackOff.UNLIMITED_ATTEMPTS);

    public virtual IMessageHeadersConverter MessageHeadersConverter { get; set; }

    public virtual IRabbitAdmin RabbitAdmin { get; set; }

    public virtual bool MissingQueuesFatal { get; set; } = true;

    public virtual bool MismatchedQueuesFatal { get; set; }

    public virtual bool PossibleAuthenticationFailureFatal { get; set; } = true;

    public virtual bool AutoDeclare { get; set; } = true;

    public virtual long FailedDeclarationRetryInterval { get; set; } = DEFAULT_FAILED_DECLARATION_RETRY_INTERVAL;

    public virtual bool StatefulRetryFatalWithNullMessageId { get; set; } = true;

    public virtual IConditionalExceptionLogger ExclusiveConsumerExceptionLogger { get; set; }

    public virtual bool AlwaysRequeueWithTxManagerRollback { get; set; }

    // Remove public string ErrorHandlerLoggerName { get; set; }
    public virtual IBatchingStrategy BatchingStrategy { get; set; }

    public virtual bool IsRunning { get; private set; }

    public virtual bool IsActive { get; private set; }

    public virtual bool IsLazyLoad { get; private set; }

    public virtual bool Initialized { get; private set; }

    public virtual IPlatformTransactionManager TransactionManager { get; set; }

    public virtual ITransactionAttribute TransactionAttribute { get; set; }

    public virtual void SetQueueNames(params string[] queueNames)
    {
        if (queueNames == null)
        {
            throw new ArgumentNullException(nameof(queueNames));
        }

        var qs = new IQueue[queueNames.Length];
        var index = 0;
        foreach (var name in queueNames)
        {
            if (name == null)
            {
                throw new ArgumentNullException("queue names cannot be null");
            }

            qs[index++] = new Queue(name);
        }

        SetQueues(qs);
    }

    public virtual string[] GetQueueNames()
    {
        return QueuesToNames().ToArray();
    }

    public virtual void SetQueues(params IQueue[] queues)
    {
        if (queues == null)
        {
            throw new ArgumentNullException(nameof(queues));
        }

        if (IsRunning)
        {
            foreach (var queue in queues)
            {
                if (queue == null)
                {
                    throw new ArgumentNullException("queue cannot be null");
                }

                if (string.IsNullOrEmpty(queue.QueueName))
                {
                    throw new ArgumentException("Cannot add broker-named queues dynamically");
                }
            }
        }

        Queues = queues.ToList();
    }

    public virtual void AddQueueNames(params string[] queueNames)
    {
        if (queueNames == null)
        {
            throw new ArgumentNullException(nameof(queueNames));
        }

        var qs = new IQueue[queueNames.Length];
        var index = 0;
        foreach (var name in queueNames)
        {
            if (name == null)
            {
                throw new ArgumentNullException("queue names cannot be null");
            }

            qs[index++] = new Queue(name);
        }

        AddQueues(qs);
    }

    public virtual void AddQueues(params IQueue[] queues)
    {
        if (queues == null)
        {
            throw new ArgumentNullException(nameof(queues));
        }

        if (IsRunning)
        {
            foreach (var queue in queues)
            {
                if (queue == null)
                {
                    throw new ArgumentNullException("queue cannot be null");
                }

                if (string.IsNullOrEmpty(queue.QueueName))
                {
                    throw new ArgumentException("Cannot add broker-named queues dynamically");
                }
            }
        }

        var newQueues = new List<IQueue>(Queues);
        newQueues.AddRange(queues);
        Queues = newQueues;
    }

    public virtual bool RemoveQueueNames(params string[] queueNames)
    {
        if (queueNames == null)
        {
            throw new ArgumentNullException(nameof(queueNames));
        }

        var toRemove = new HashSet<string>();
        foreach (var name in queueNames)
        {
            if (name == null)
            {
                throw new ArgumentNullException("queue names cannot be null");
            }

            toRemove.Add(name);
        }

        var copy = new List<IQueue>(Queues);
        var filtered = copy.Where(q => !toRemove.Contains(q.ActualName)).ToList();
        Queues = filtered;
        return filtered.Count != copy.Count;
    }

    public virtual void RemoveQueues(params IQueue[] queues)
    {
        if (queues == null)
        {
            throw new ArgumentNullException(nameof(queues));
        }

        var toRemove = new string[queues.Length];
        var index = 0;
        foreach (var queue in queues)
        {
            if (queue == null)
            {
                throw new ArgumentNullException("queue cannot be null");
            }

            toRemove[index++] = queue.ActualName;
        }

        RemoveQueueNames(toRemove);
    }

    public virtual void SetAfterReceivePostProcessors(params IMessagePostProcessor[] afterReceivePostProcessors)
    {
        if (afterReceivePostProcessors == null)
        {
            throw new ArgumentNullException(nameof(afterReceivePostProcessors));
        }

        var asList = new List<IMessagePostProcessor>();
        foreach (var p in afterReceivePostProcessors)
        {
            if (p == null)
            {
                throw new ArgumentNullException("'afterReceivePostProcessors' cannot have null elements");
            }

            asList.Add(p);
        }

        AfterReceivePostProcessors = MessagePostProcessorUtils.Sort(asList);
    }

    public virtual void AddAfterReceivePostProcessors(params IMessagePostProcessor[] afterReceivePostProcessors)
    {
        if (afterReceivePostProcessors == null)
        {
            throw new ArgumentNullException(nameof(afterReceivePostProcessors));
        }

        var current = AfterReceivePostProcessors ?? new List<IMessagePostProcessor>();

        var asList = afterReceivePostProcessors.ToList();
        asList.AddRange(current);
        AfterReceivePostProcessors = MessagePostProcessorUtils.Sort(asList);
    }

    public virtual bool RemoveAfterReceivePostProcessor(IMessagePostProcessor afterReceivePostProcessor)
    {
        if (afterReceivePostProcessor == null)
        {
            throw new ArgumentNullException(nameof(afterReceivePostProcessor));
        }

        var current = AfterReceivePostProcessors;
        if (current != null && current.Contains(afterReceivePostProcessor))
        {
            var copy = new List<IMessagePostProcessor>(current);
            copy.Remove(afterReceivePostProcessor);
            AfterReceivePostProcessors = copy;
            return true;
        }

        return false;
    }

    public virtual void SetupMessageListener(IMessageListener messageListener)
    {
        MessageListener = messageListener;
    }

    public virtual IConnectionFactory GetConnectionFactory()
    {
        var connectionFactory = ConnectionFactory;
        if (connectionFactory is IRoutingConnectionFactory routingFactory)
        {
            var targetConnectionFactory = routingFactory.GetTargetConnectionFactory(GetRoutingLookupKey());
            if (targetConnectionFactory != null)
            {
                return targetConnectionFactory;
            }
        }

        return connectionFactory;
    }

    public virtual void Initialize()
    {
        ValidateConfiguration();
        try
        {
            lock (_lifecycleMonitor)
            {
                Monitor.PulseAll(_lifecycleMonitor);
            }

            CheckMissingQueuesFatalFromProperty();
            CheckPossibleAuthenticationFailureFatalFromProperty();
            DoInitialize();

            if (!ExposeListenerChannel && TransactionManager != null)
            {
                _logger?.LogWarning("exposeListenerChannel=false is ignored when using a TransactionManager");
            }

            if (TransactionManager != null && !IsChannelTransacted)
            {
                _logger?.LogDebug("The 'channelTransacted' is coerced to 'true', when 'transactionManager' is provided");
                IsChannelTransacted = true;
            }

            if (MessageListener != null)
            {
                MessageListener.ContainerAckMode = AcknowledgeMode;
            }

            Initialized = true;
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error initializing listener container");
            throw ConvertRabbitAccessException(e);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Shutdown();
        }
    }

    public virtual void Shutdown()
    {
        lock (_lifecycleMonitor)
        {
            if (!IsActive)
            {
                _logger?.LogInformation("Shutdown ignored - container is not active already");
                return;
            }

            IsActive = false;
            Monitor.PulseAll(_lifecycleMonitor);
        }

        _logger?.LogDebug("Shutting down RabbitMQ listener container");

        // Shut down the invokers.
        try
        {
            DoShutdown();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "DoShutdown error");
            throw ConvertRabbitAccessException(ex);
        }
        finally
        {
            lock (_lifecycleMonitor)
            {
                IsRunning = false;
                Monitor.PulseAll(_lifecycleMonitor);
            }
        }
    }

    public virtual Task Start()
    {
        if (IsRunning)
        {
            return Task.CompletedTask;
        }

        if (!Initialized)
        {
            lock (_lifecycleMonitor)
            {
                if (!Initialized)
                {
                    Initialize();
                }
            }
        }

        try
        {
            _logger?.LogDebug("Starting RabbitMQ listener container {name}", ServiceName);
            ConfigureAdminIfNeeded();
            CheckMismatchedQueues();
            DoStart();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error Starting RabbitMQ listener container {name}", ServiceName);
            throw ConvertRabbitAccessException(ex);
        }
        finally
        {
            IsLazyLoad = false;
        }

        return Task.CompletedTask;
    }

    public virtual Task Stop()
    {
        try
        {
            DoStop();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping RabbitMQ listener container {name}", ServiceName);
            throw ConvertRabbitAccessException(ex);
        }
        finally
        {
            lock (_lifecycleMonitor)
            {
                IsRunning = false;
                Monitor.PulseAll(_lifecycleMonitor);
            }
        }

        return Task.CompletedTask;
    }

    public virtual Task Stop(Action callback)
    {
        try
        {
            Stop();
        }
        finally
        {
            callback();
        }

        return Task.CompletedTask;
    }

    public virtual void LazyLoad()
    {
        if (MismatchedQueuesFatal)
        {
            if (MissingQueuesFatal)
            {
                _logger?.LogWarning("'mismatchedQueuesFatal' and 'missingQueuesFatal' are ignored during the initial start(), "
                                    + "for lazily loaded containers");
            }
            else
            {
                _logger?.LogWarning("'mismatchedQueuesFatal' is ignored during the initial start(), " + "for lazily loaded containers");
            }
        }
        else if (MissingQueuesFatal)
        {
            _logger?.LogWarning("'missingQueuesFatal' is ignored during the initial start(), "
                                + "for lazily loaded containers");
        }

        IsLazyLoad = true;
    }

    protected virtual IConnection CreateConnection()
    {
        return ConnectionFactory.CreateConnection();
    }

    protected virtual RabbitResourceHolder GetTransactionalResourceHolder()
    {
        return ConnectionFactoryUtils.GetTransactionalResourceHolder(ConnectionFactory, IsChannelTransacted);
    }

    protected virtual Exception ConvertRabbitAccessException(Exception ex)
    {
        return RabbitExceptionTranslator.ConvertRabbitAccessException(ex);
    }

    protected virtual void RedeclareElementsIfNecessary()
    {
        lock (_lock)
        {
            var admin = RabbitAdmin;
            if (!IsLazyLoad && admin != null && AutoDeclare)
            {
                try
                {
                    AttemptDeclarations(admin);
                }
                catch (Exception e)
                {
                    if (RabbitUtils.IsMismatchedQueueArgs(e) && MismatchedQueuesFatal)
                    {
                        throw new FatalListenerStartupException("Mismatched queues", e);
                    }

                    _logger?.LogError(e, "Failed to check/redeclare auto-delete queue(s). Container: {name}", ServiceName);
                }
            }
        }
    }

    protected virtual void InvokeErrorHandler(Exception ex)
    {
        if (ErrorHandler != null)
        {
            try
            {
                ErrorHandler.HandleError(ex);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Execution of Rabbit message listener failed, and the error handler threw an exception. Container: {name}", ServiceName);
                throw;
            }
        }
        else
        {
            _logger?.LogWarning(ex, "Execution of Rabbit message listener failed, and no ErrorHandler has been set. Container: {name}", ServiceName);
        }
    }

    protected virtual void ExecuteListener(R.IModel channel, IMessage message)
    {
        if (!IsRunning)
        {
            _logger?.LogWarning("Rejecting received message(s) because the listener container {name} has been stopped {message}", ServiceName, message);
            throw new MessageRejectedWhileStoppingException();
        }

        try
        {
            DoExecuteListener(channel, message);
        }
        catch (Exception ex)
        {
            CheckStatefulRetry(ex, message);
            HandleListenerException(ex);
            throw;
        }
    }

    protected virtual void ActualInvokeListener(R.IModel channel, List<IMessage> data)
    {
        var listener = MessageListener;
        if (listener is IChannelAwareMessageListener chanListener)
        {
            DoInvokeListener(chanListener, channel, data);
        }
        else if (listener != null)
        {
            var bindChannel = ExposeListenerChannel && IsChannelLocallyTransacted;
            if (bindChannel)
            {
                var resourceHolder = new RabbitResourceHolder(channel, false, _loggerFactory?.CreateLogger<RabbitResourceHolder>())
                {
                    SynchronizedWithTransaction = true
                };
                TransactionSynchronizationManager.BindResource(ConnectionFactory, resourceHolder);
            }

            try
            {
                DoInvokeListener(listener, data);
            }
            finally
            {
                if (bindChannel)
                {
                    // unbind if we bound
                    TransactionSynchronizationManager.UnbindResource(ConnectionFactory);
                }
            }
        }
        else
        {
            throw new FatalListenerExecutionException("No message listener specified - see property 'messageListener'");
        }
    }

    protected virtual void ActualInvokeListener(R.IModel channel, IMessage message)
    {
        var listener = MessageListener;
        if (listener is IChannelAwareMessageListener chanListener)
        {
            DoInvokeListener(chanListener, channel, message);
        }
        else if (listener != null)
        {
            var bindChannel = ExposeListenerChannel && IsChannelLocallyTransacted;
            if (bindChannel)
            {
                var resourceHolder = new RabbitResourceHolder(channel, false, _loggerFactory?.CreateLogger<RabbitResourceHolder>())
                {
                    SynchronizedWithTransaction = true
                };
                TransactionSynchronizationManager.BindResource(ConnectionFactory, resourceHolder);
            }

            try
            {
                DoInvokeListener(listener, message);
            }
            finally
            {
                if (bindChannel)
                {
                    // unbind if we bound
                    TransactionSynchronizationManager.UnbindResource(ConnectionFactory);
                }
            }
        }
        else
        {
            throw new FatalListenerExecutionException("No message listener specified - see property 'messageListener'");
        }
    }

    protected virtual void DoInvokeListener(IChannelAwareMessageListener listener, R.IModel channel, List<IMessage> data)
    {
        RabbitResourceHolder resourceHolder = null;
        var channelToUse = channel;
        var boundHere = false;
        try
        {
            boundHere = HandleChannelAwareTransaction(channel, out channelToUse, out resourceHolder);

            // Actually invoke the message listener...
            try
            {
                listener.OnMessageBatch(data, channelToUse);
            }
            catch (Exception e)
            {
                throw WrapToListenerExecutionFailedExceptionIfNeeded(e, data);
            }
        }
        finally
        {
            CleanUpAfterInvoke(resourceHolder, channelToUse, boundHere);
        }
    }

    protected virtual void DoInvokeListener(IChannelAwareMessageListener listener, R.IModel channel, IMessage message)
    {
        RabbitResourceHolder resourceHolder = null;
        var channelToUse = channel;
        var boundHere = false;
        try
        {
            boundHere = HandleChannelAwareTransaction(channel, out channelToUse, out resourceHolder);

            // Actually invoke the message listener...
            try
            {
                listener.OnMessage(message, channelToUse);
            }
            catch (Exception e)
            {
                throw WrapToListenerExecutionFailedExceptionIfNeeded(e, message);
            }
        }
        finally
        {
            CleanUpAfterInvoke(resourceHolder, channelToUse, boundHere);
        }
    }

    protected virtual void DoInvokeListener(IMessageListener listener, List<IMessage> data)
    {
        try
        {
            listener.OnMessageBatch(data);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Exception in OnMessage call. Container: {name}", ServiceName);
            throw WrapToListenerExecutionFailedExceptionIfNeeded(e, data);
        }
    }

    protected virtual void DoInvokeListener(IMessageListener listener, IMessage message)
    {
        try
        {
            listener.OnMessage(message);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Exception in OnMessage call. Container: {name}", ServiceName);
            throw WrapToListenerExecutionFailedExceptionIfNeeded(e, message);
        }
    }

    protected virtual bool HandleChannelAwareTransaction(R.IModel channel, out R.IModel channelToUse, out RabbitResourceHolder resourceHolder)
    {
        resourceHolder = null;
        channelToUse = channel;
        var boundHere = false;
        if (!ExposeListenerChannel)
        {
            // We need to expose a separate Channel.
            resourceHolder = GetTransactionalResourceHolder();
            channelToUse = resourceHolder.GetChannel();
            /*
             * If there is a real transaction, the resource will have been bound; otherwise
             * we need to bind it temporarily here. Any work done on this channel
             * will be committed in the finally block.
             */
            if (IsChannelLocallyTransacted &&
                !TransactionSynchronizationManager.IsActualTransactionActive())
            {
                resourceHolder.SynchronizedWithTransaction = true;
                TransactionSynchronizationManager.BindResource(ConnectionFactory, resourceHolder);
                boundHere = true;
            }
        }
        else
        {
            // if locally transacted, bind the current channel to make it available to RabbitTemplate
            if (IsChannelLocallyTransacted)
            {
                var localResourceHolder = new RabbitResourceHolder(channelToUse, false, _loggerFactory?.CreateLogger<RabbitResourceHolder>())
                {
                    SynchronizedWithTransaction = true
                };
                TransactionSynchronizationManager.BindResource(ConnectionFactory, localResourceHolder);
                boundHere = true;
            }
        }

        return boundHere;
    }

    protected virtual ListenerExecutionFailedException WrapToListenerExecutionFailedExceptionIfNeeded(Exception exception, List<IMessage> data)
    {
        if (exception is not ListenerExecutionFailedException listenerException)
        {
            return new ListenerExecutionFailedException("Listener threw exception", exception, data.ToArray());
        }

        return listenerException;
    }

    protected virtual ListenerExecutionFailedException WrapToListenerExecutionFailedExceptionIfNeeded(Exception exception, IMessage message)
    {
        if (exception is not ListenerExecutionFailedException listenerException)
        {
            return new ListenerExecutionFailedException("Listener threw exception", exception, message);
        }

        return listenerException;
    }

    protected virtual void HandleListenerException(Exception exception)
    {
        if (IsActive)
        {
            // Regular case: failed while active.
            // Invoke ErrorHandler if available.
            InvokeErrorHandler(exception);
        }
        else
        {
            // Rare case: listener thread failed after container shutdown.
            // Log at debug level, to avoid spamming the shutdown log.
            _logger?.LogDebug(exception, "Listener exception after container shutdown. Container: {name}", ServiceName);
        }
    }

    protected virtual void UpdateLastReceive()
    {
        if (IdleEventInterval > 0)
        {
            LastReceive = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }
    }

    protected virtual void ConfigureAdminIfNeeded()
    {
        if (RabbitAdmin == null && ApplicationContext != null)
        {
            var admins = ApplicationContext.GetServices<IRabbitAdmin>();
            if (admins.Count() == 1)
            {
                RabbitAdmin = admins.Single();
            }
            else
            {
                if (AutoDeclare || MismatchedQueuesFatal)
                {
                    _logger?.LogDebug(
                        "For 'autoDeclare' and 'mismatchedQueuesFatal' to work, there must be exactly one "
                        + "RabbitAdmin in the context or you must inject one into this container; found: {count}"
                        + " for container {container}",
                        admins.Count(),
                        ToString());
                }

                if (MismatchedQueuesFatal)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            "When 'mismatchedQueuesFatal' is 'true', there must be exactly "
                            + "one RabbitAdmin in the context or you must inject one into this container; found: {0} "
                            + " for container {1}",
                            admins.Count(),
                            ToString()));
                }
            }
        }
    }

    protected virtual void CheckMismatchedQueues()
    {
        if (MismatchedQueuesFatal && RabbitAdmin != null)
        {
            try
            {
                RabbitAdmin.Initialize();
            }
            catch (RabbitConnectException e)
            {
                _logger?.LogInformation(e, "Broker not available; cannot check queue declarations. Container: {name}", ServiceName);
            }
            catch (RabbitIOException e)
            {
                if (RabbitUtils.IsMismatchedQueueArgs(e))
                {
                    throw new FatalListenerStartupException("Mismatched queues", e);
                }
                else
                {
                    _logger?.LogInformation(e, "Failed to get connection during Start()");
                }
            }
        }
        else
        {
            try
            {
                var connection = ConnectionFactory.CreateConnection();
                if (connection != null)
                {
                    connection.Close();
                }
            }
            catch (Exception e)
            {
                _logger?.LogInformation(e, "Broker not available; cannot force queue declarations during start");
            }
        }
    }

    protected virtual bool IsChannelLocallyTransacted => IsChannelTransacted && TransactionManager == null;

    protected abstract void DoInitialize();

    protected abstract void DoShutdown();

    protected virtual void DoStart()
    {
        // Reschedule paused tasks, if any.
        lock (_lifecycleMonitor)
        {
            IsActive = true;
            IsRunning = true;
            Monitor.PulseAll(_lifecycleMonitor);
        }
    }

    protected virtual void DoStop()
    {
        Shutdown();
    }

    protected virtual void ValidateConfiguration()
    {
        if (!(ExposeListenerChannel || !AcknowledgeMode.IsManual()))
        {
            throw new ArgumentException(
                "You cannot acknowledge messages manually if the channel is not exposed to the listener "
                + "(please check your configuration and set exposeListenerChannel=true or " +
                "acknowledgeMode!=MANUAL)");
        }

        if (IsChannelTransacted && AcknowledgeMode.IsAutoAck())
        {
            throw new ArgumentException(
                "The acknowledgeMode is NONE (autoack in Rabbit terms) which is not consistent with having a "
                + "transactional channel. Either use a different AcknowledgeMode or make sure " +
                "channelTransacted=false");
        }
    }

    protected virtual IRoutingConnectionFactory GetRoutingConnectionFactory()
    {
        return ConnectionFactory as IRoutingConnectionFactory;
    }

    protected virtual long LastReceive { get; private set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();

    protected virtual bool ForceCloseChannel { get; set; } = true;

    protected virtual string GetRoutingLookupKey()
    {
        return ConnectionFactory is IRoutingConnectionFactory ? LookupKeyQualifier + GetQueuesAsListString() : null;
    }

    protected virtual void CheckMessageListener(object listener)
    {
        if (listener is not IMessageListener)
        {
            throw new ArgumentException($"Message listener needs to be of type [{nameof(IMessageListener)}] or [{nameof(IChannelAwareMessageListener)}]");
        }
    }

    protected virtual ISet<string> GetQueueNamesAsSet()
    {
        return new HashSet<string>(QueuesToNames());
    }

    protected virtual Dictionary<string, IQueue> GetQueueNamesToQueues()
    {
        return Queues.ToDictionary(q => q.ActualName);
    }

    protected virtual bool CauseChainHasImmediateAcknowledgeRabbitException(Exception exception)
    {
        // if (ex instanceof Error) {
        //    return false;
        // }
        var cause = exception.InnerException;
        while (cause != null)
        {
            if (cause is ImmediateAcknowledgeException)
            {
                return true;
            }
            else if (cause is RabbitRejectAndDontRequeueException)
            {
                return false;
            }

            cause = cause.InnerException;
        }

        return false;
    }

    protected virtual void PrepareHolderForRollback(RabbitResourceHolder resourceHolder, Exception exception)
    {
        if (resourceHolder != null)
        {
            resourceHolder.RequeueOnRollback = AlwaysRequeueWithTxManagerRollback ||
                                               ContainerUtils.ShouldRequeue(DefaultRequeueRejected, exception, _logger);
        }
    }

    private void AttemptDeclarations(IRabbitAdmin admin)
    {
        var queueNames = GetQueueNamesAsSet();
        var queueBeans = ApplicationContext.GetServices<IQueue>();
        foreach (var entry in queueBeans)
        {
            if (MismatchedQueuesFatal || (queueNames.Contains(entry.QueueName) && admin.GetQueueProperties(entry.QueueName) == null))
            {
                _logger?.LogDebug("Redeclaring context exchanges, queues, bindings. Container: {name}", ServiceName);
                admin.Initialize();
                break;
            }
        }
    }

    private void CheckStatefulRetry(Exception ex, IMessage message)
    {
        if (message.Headers.IsFinalRetryForMessageWithNoId())
        {
            if (StatefulRetryFatalWithNullMessageId)
            {
                throw new FatalListenerExecutionException($"Illegal null id in message. Failed to manage retry for message: {message}", ex);
            }
            else
            {
                throw new ListenerExecutionFailedException(
                    "Cannot retry message more than once without an ID",
                    new RabbitRejectAndDontRequeueException("Not retryable; rejecting and not requeuing", ex),
                    message);
            }
        }
    }

    private void CleanUpAfterInvoke(RabbitResourceHolder resourceHolder, R.IModel channelToUse, bool boundHere)
    {
        if (resourceHolder != null && boundHere)
        {
            // so the channel exposed (because exposeListenerChannel is false) will be closed
            resourceHolder.SynchronizedWithTransaction = false;
        }

        ConnectionFactoryUtils.ReleaseResources(resourceHolder);
        if (boundHere)
        {
            // unbind if we bound
            TransactionSynchronizationManager.UnbindResource(ConnectionFactory);
            if (!ExposeListenerChannel && IsChannelLocallyTransacted)
            {
                /*
                 *  commit the temporary channel we exposed; the consumer's channel
                 *  will be committed later. Note that when exposing a different channel
                 *  when there's no transaction manager, the exposed channel is committed
                 *  on each message, and not based on txSize.
                 */
                RabbitUtils.CommitIfNecessary(channelToUse, _logger);
            }
        }
    }

    private void DoExecuteListener(R.IModel channel, IMessage message)
    {
        if (AfterReceivePostProcessors != null)
        {
            var postProcessed = message;
            foreach (var processor in AfterReceivePostProcessors)
            {
                postProcessed = processor.PostProcessMessage(postProcessed);
                if (postProcessed == null)
                {
                    throw new ImmediateAcknowledgeException("Message Post Processor returned 'null', discarding message");
                }
            }

            message = postProcessed as IMessage<byte[]>;
            if (message == null)
            {
                throw new InvalidOperationException("AfterReceivePostProcessors failed to return a IMessage<byte[]>");
            }
        }

        if (IsDeBatchingEnabled && BatchingStrategy.CanDebatch(message.Headers))
        {
            BatchingStrategy.DeBatch(message, fragment => ActualInvokeListener(channel, fragment));
        }
        else
        {
            ActualInvokeListener(channel, message);
        }
    }

    private string GetQueuesAsListString()
    {
        var sb = new StringBuilder("[");
        var queues = Queues;
        foreach (var q in queues)
        {
            sb.Append(q.QueueName);
            sb.Append(',');
        }

        return $"{sb.ToString(0, sb.Length - 1)}]";
    }

    private List<string> QueuesToNames()
    {
        return Queues.Select(q => q.ActualName).ToList();
    }

    private void CheckMissingQueuesFatalFromProperty()
    {
        // TODO: Decide to support these global settings?
        // if (!MissingQueuesFatalSet)
        //            {
        //                try
        //                {
        //                    ApplicationContext context = getApplicationContext();
        //                    if (context != null)
        //                    {
        //                        Properties properties = context.getBean("spring.amqp.global.properties", Properties.class);
        // String missingQueuesFatalProperty = properties.getProperty("mlc.missing.queues.fatal");

        // if (!StringUtils.hasText(missingQueuesFatalProperty)) {
        // missingQueuesFatalProperty = properties.getProperty("smlc.missing.queues.fatal");
        // }

        // if (StringUtils.hasText(missingQueuesFatalProperty)) {
        // setMissingQueuesFatal(Boolean.parseBoolean(missingQueuesFatalProperty));
        // }
        // }
        // }
        // catch (BeansException be) {
        // logger.debug("No global properties bean");
        // }
        // }
    }

    private void CheckPossibleAuthenticationFailureFatalFromProperty()
    {
        // TODO: Decide to support these global settings?
        // if (!isPossibleAuthenticationFailureFatalSet())
        //            {
        //                try
        //                {
        //                    ApplicationContext context = getApplicationContext();
        //                    if (context != null)
        //                    {
        //                        Properties properties = context.getBean("spring.amqp.global.properties", Properties.class);
        // String possibleAuthenticationFailureFatalProperty =
        //                            properties.getProperty("mlc.possible.authentication.failure.fatal");
        // if (StringUtils.hasText(possibleAuthenticationFailureFatalProperty)) {
        // setPossibleAuthenticationFailureFatal(
        //                                Boolean.parseBoolean(possibleAuthenticationFailureFatalProperty));
        //    }
        // }
        // }
        // catch (BeansException be) {
        // logger.debug("No global properties bean");
        // }
        // }
    }

    private sealed class DefaultExclusiveConsumerLogger : IConditionalExceptionLogger
    {
        public void Log(ILogger logger, string message, object cause)
        {
            logger.LogError("Unexpected invocation of {type}, with {message}:{cause}", GetType(), message, cause);
        }
    }
}
