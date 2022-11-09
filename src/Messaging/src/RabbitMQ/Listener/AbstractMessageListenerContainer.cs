// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Transaction;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Batch;
using Steeltoe.Messaging.RabbitMQ.Configuration;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Listener.Support;
using Steeltoe.Messaging.RabbitMQ.Support;
using R = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

public abstract class AbstractMessageListenerContainer : IMessageListenerContainer
{
    public const int DefaultFailedDeclarationRetryInterval = 5000;
    public const long DefaultShutdownTimeout = 5000;
    public const int DefaultRecoveryInterval = 5000;
    public const bool DefaultDebatchingEnabled = true;
    public const int DefaultPrefetchCount = 250;

    protected readonly object ConsumersMonitor = new();
    protected readonly object Lock = new();
    protected readonly object LifecycleMonitor = new();
    protected readonly ILogger Logger;
    protected readonly ILoggerFactory LoggerFactory;
    private string _listenerId;
    private IConnectionFactory _connectionFactory;

    protected int recoveryInterval = DefaultRecoveryInterval;

    private List<IQueue> Queues { get; set; } = new();

    protected virtual bool IsChannelLocallyTransacted => IsChannelTransacted && TransactionManager == null;

    protected virtual long LastReceive { get; private set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();

    protected virtual bool ForceCloseChannel { get; set; } = true;

    public virtual IConnectionFactory ConnectionFactory
    {
        get
        {
            _connectionFactory ??= ApplicationContext.GetService<IConnectionFactory>();
            return _connectionFactory;
        }
        set => _connectionFactory = value;
    }

    public virtual bool IsChannelTransacted { get; set; }

    public IApplicationContext ApplicationContext { get; set; }

    public virtual AcknowledgeMode AcknowledgeMode { get; set; } = AcknowledgeMode.Auto;

    public virtual string ServiceName { get; set; }

    public virtual bool ExposeListenerChannel { get; set; } = true;

    public virtual IMessageListener MessageListener { get; set; }

    public virtual IErrorHandler ErrorHandler { get; set; }

    public virtual bool IsDeBatchingEnabled { get; set; } = DefaultDebatchingEnabled;

    public virtual IList<IMessagePostProcessor> AfterReceivePostProcessors { get; private set; }

    public virtual bool IsAutoStartup { get; set; } = true;

    public virtual int Phase { get; set; } = int.MaxValue;

    public virtual string LookupKeyQualifier { get; set; } = string.Empty;

    public virtual string ListenerId
    {
        get => _listenerId ?? ServiceName;
        set => _listenerId = value;
    }

    public virtual IConsumerTagStrategy ConsumerTagStrategy { get; set; }

    public virtual Dictionary<string, object> ConsumerArguments { get; set; } = new();

    public virtual bool Exclusive { get; set; }

    public virtual bool NoLocal { get; set; }

    public virtual bool DefaultRequeueRejected { get; set; } = true;

    public virtual int PrefetchCount { get; set; } = DefaultPrefetchCount;

    public virtual long ShutdownTimeout { get; set; } = DefaultShutdownTimeout;

    public virtual long IdleEventInterval { get; set; }

    public virtual int RecoveryInterval
    {
        get => recoveryInterval;
        set
        {
            recoveryInterval = value;
            RecoveryBackOff = new FixedBackOff(recoveryInterval, FixedBackOff.UnlimitedAttempts);
        }
    }

    public IBackOff RecoveryBackOff { get; set; } = new FixedBackOff(DefaultRecoveryInterval, FixedBackOff.UnlimitedAttempts);

    public virtual IMessageHeadersConverter MessageHeadersConverter { get; set; }

    public virtual IRabbitAdmin RabbitAdmin { get; set; }

    public virtual bool MissingQueuesFatal { get; set; } = true;

    public virtual bool MismatchedQueuesFatal { get; set; }

    public virtual bool PossibleAuthenticationFailureFatal { get; set; } = true;

    public virtual bool AutoDeclare { get; set; } = true;

    public virtual long FailedDeclarationRetryInterval { get; set; } = DefaultFailedDeclarationRetryInterval;

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

    protected AbstractMessageListenerContainer(IApplicationContext applicationContext, IConnectionFactory connectionFactory, string name = null,
        ILoggerFactory loggerFactory = null)
    {
        LoggerFactory = loggerFactory;
        Logger = LoggerFactory?.CreateLogger(GetType());
        ApplicationContext = applicationContext;
        ConnectionFactory = connectionFactory;
        ErrorHandler = new ConditionalRejectingErrorHandler(Logger);
        MessageHeadersConverter = new DefaultMessageHeadersConverter(Logger);
        ExclusiveConsumerExceptionLogger = new DefaultExclusiveConsumerLogger();
        BatchingStrategy = new SimpleBatchingStrategy(0, 0, 0L);
        TransactionAttribute = new DefaultTransactionAttribute();
        ServiceName = name ?? $"{GetType().Name}@{GetHashCode()}";
    }

    public virtual void SetQueueNames(params string[] queueNames)
    {
        ArgumentGuard.NotNull(queueNames);
        ArgumentGuard.ElementsNotNull(queueNames);

        IQueue[] queues = queueNames.Select(name => new Queue(name)).Cast<IQueue>().ToArray();
        SetQueues(queues);
    }

    public virtual string[] GetQueueNames()
    {
        return QueuesToNames().ToArray();
    }

    public virtual void SetQueues(params IQueue[] queues)
    {
        ArgumentGuard.NotNull(queues);
        ArgumentGuard.ElementsNotNull(queues);

        if (IsRunning && queues.Any(queue => queue.QueueName == string.Empty))
        {
            throw new InvalidOperationException("Cannot add broker-named queues dynamically.");
        }

        Queues = queues.ToList();
    }

    public virtual void AddQueueNames(params string[] queueNames)
    {
        ArgumentGuard.NotNull(queueNames);
        ArgumentGuard.ElementsNotNull(queueNames);

        IQueue[] queues = queueNames.Select(name => new Queue(name)).Cast<IQueue>().ToArray();
        AddQueues(queues);
    }

    public virtual void AddQueues(params IQueue[] queues)
    {
        ArgumentGuard.NotNull(queues);
        ArgumentGuard.ElementsNotNull(queues);

        if (IsRunning && queues.Any(queue => queue.QueueName == string.Empty))
        {
            throw new InvalidOperationException("Cannot add broker-named queues dynamically.");
        }

        var newQueues = new List<IQueue>(Queues);
        newQueues.AddRange(queues);
        Queues = newQueues;
    }

    public virtual bool RemoveQueueNames(params string[] queueNames)
    {
        ArgumentGuard.NotNull(queueNames);
        ArgumentGuard.ElementsNotNull(queueNames);

        HashSet<string> toRemove = queueNames.ToHashSet();
        var copy = new List<IQueue>(Queues);
        List<IQueue> filtered = copy.Where(q => !toRemove.Contains(q.ActualName)).ToList();
        Queues = filtered;
        return filtered.Count != copy.Count;
    }

    public virtual void RemoveQueues(params IQueue[] queues)
    {
        ArgumentGuard.NotNull(queues);
        ArgumentGuard.ElementsNotNull(queues);

        string[] toRemove = queues.Select(queue => queue.ActualName).ToArray();
        RemoveQueueNames(toRemove);
    }

    public virtual void SetAfterReceivePostProcessors(params IMessagePostProcessor[] afterReceivePostProcessors)
    {
        ArgumentGuard.NotNull(afterReceivePostProcessors);
        ArgumentGuard.ElementsNotNull(afterReceivePostProcessors);

        List<IMessagePostProcessor> asList = afterReceivePostProcessors.ToList();
        AfterReceivePostProcessors = MessagePostProcessorUtils.Sort(asList);
    }

    public virtual void AddAfterReceivePostProcessors(params IMessagePostProcessor[] afterReceivePostProcessors)
    {
        ArgumentGuard.NotNull(afterReceivePostProcessors);
        ArgumentGuard.ElementsNotNull(afterReceivePostProcessors);

        IList<IMessagePostProcessor> current = AfterReceivePostProcessors ?? new List<IMessagePostProcessor>();

        List<IMessagePostProcessor> asList = afterReceivePostProcessors.ToList();
        asList.AddRange(current);
        AfterReceivePostProcessors = MessagePostProcessorUtils.Sort(asList);
    }

    public virtual bool RemoveAfterReceivePostProcessor(IMessagePostProcessor afterReceivePostProcessor)
    {
        ArgumentGuard.NotNull(afterReceivePostProcessor);

        IList<IMessagePostProcessor> current = AfterReceivePostProcessors;

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
        IConnectionFactory connectionFactory = ConnectionFactory;

        if (connectionFactory is IRoutingConnectionFactory routingFactory)
        {
            IConnectionFactory targetConnectionFactory = routingFactory.GetTargetConnectionFactory(GetRoutingLookupKey());

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
            lock (LifecycleMonitor)
            {
                Monitor.PulseAll(LifecycleMonitor);
            }

            CheckMissingQueuesFatalFromProperty();
            CheckPossibleAuthenticationFailureFatalFromProperty();
            DoInitialize();

            if (!ExposeListenerChannel && TransactionManager != null)
            {
                Logger?.LogWarning("exposeListenerChannel=false is ignored when using a TransactionManager");
            }

            if (TransactionManager != null && !IsChannelTransacted)
            {
                Logger?.LogDebug("The 'channelTransacted' is coerced to 'true', when 'transactionManager' is provided");
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
            Logger?.LogError(e, "Error initializing listener container");
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
        lock (LifecycleMonitor)
        {
            if (!IsActive)
            {
                Logger?.LogInformation("Shutdown ignored - container is not active already");
                return;
            }

            IsActive = false;
            Monitor.PulseAll(LifecycleMonitor);
        }

        Logger?.LogDebug("Shutting down RabbitMQ listener container");

        // Shut down the invokers.
        try
        {
            DoShutdown();
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "DoShutdown error");
            throw ConvertRabbitAccessException(ex);
        }
        finally
        {
            lock (LifecycleMonitor)
            {
                IsRunning = false;
                Monitor.PulseAll(LifecycleMonitor);
            }
        }
    }

    public virtual Task StartAsync()
    {
        if (IsRunning)
        {
            return Task.CompletedTask;
        }

        if (!Initialized)
        {
            lock (LifecycleMonitor)
            {
                if (!Initialized)
                {
                    Initialize();
                }
            }
        }

        try
        {
            Logger?.LogDebug("Starting RabbitMQ listener container {name}", ServiceName);
            ConfigureAdminIfNeeded();
            CheckMismatchedQueues();
            DoStart();
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error Starting RabbitMQ listener container {name}", ServiceName);
            throw ConvertRabbitAccessException(ex);
        }
        finally
        {
            IsLazyLoad = false;
        }

        return Task.CompletedTask;
    }

    public virtual Task StopAsync()
    {
        try
        {
            DoStop();
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error stopping RabbitMQ listener container {name}", ServiceName);
            throw ConvertRabbitAccessException(ex);
        }
        finally
        {
            lock (LifecycleMonitor)
            {
                IsRunning = false;
                Monitor.PulseAll(LifecycleMonitor);
            }
        }

        return Task.CompletedTask;
    }

    public virtual Task StopAsync(Action callback)
    {
        try
        {
            StopAsync();
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
                Logger?.LogWarning("'mismatchedQueuesFatal' and 'missingQueuesFatal' are ignored during the initial start(), for lazily loaded containers");
            }
            else
            {
                Logger?.LogWarning("'mismatchedQueuesFatal' is ignored during the initial start(), for lazily loaded containers");
            }
        }
        else if (MissingQueuesFatal)
        {
            Logger?.LogWarning("'missingQueuesFatal' is ignored during the initial start(), for lazily loaded containers");
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
        lock (Lock)
        {
            IRabbitAdmin admin = RabbitAdmin;

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

                    Logger?.LogError(e, "Failed to check/redeclare auto-delete queue(s). Container: {name}", ServiceName);
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
                Logger?.LogError(e, "Execution of Rabbit message listener failed, and the error handler threw an exception. Container: {name}", ServiceName);
                throw;
            }
        }
        else
        {
            Logger?.LogWarning(ex, "Execution of Rabbit message listener failed, and no ErrorHandler has been set. Container: {name}", ServiceName);
        }
    }

    protected virtual void ExecuteListener(R.IModel channel, IMessage message)
    {
        if (!IsRunning)
        {
            Logger?.LogWarning("Rejecting received message(s) because the listener container {name} has been stopped {message}", ServiceName, message);
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
        IMessageListener listener = MessageListener;

        if (listener is IChannelAwareMessageListener chanListener)
        {
            DoInvokeListener(chanListener, channel, data);
        }
        else if (listener != null)
        {
            bool bindChannel = ExposeListenerChannel && IsChannelLocallyTransacted;

            if (bindChannel)
            {
                var resourceHolder = new RabbitResourceHolder(channel, false, LoggerFactory?.CreateLogger<RabbitResourceHolder>())
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
        IMessageListener listener = MessageListener;

        if (listener is IChannelAwareMessageListener chanListener)
        {
            DoInvokeListener(chanListener, channel, message);
        }
        else if (listener != null)
        {
            bool bindChannel = ExposeListenerChannel && IsChannelLocallyTransacted;

            if (bindChannel)
            {
                var resourceHolder = new RabbitResourceHolder(channel, false, LoggerFactory?.CreateLogger<RabbitResourceHolder>())
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
        R.IModel channelToUse = channel;
        bool boundHere = false;

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
        R.IModel channelToUse = channel;
        bool boundHere = false;

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
            Logger?.LogError(e, "Exception in OnMessage call. Container: {name}", ServiceName);
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
            Logger?.LogError(e, "Exception in OnMessage call. Container: {name}", ServiceName);
            throw WrapToListenerExecutionFailedExceptionIfNeeded(e, message);
        }
    }

    protected virtual bool HandleChannelAwareTransaction(R.IModel channel, out R.IModel channelToUse, out RabbitResourceHolder resourceHolder)
    {
        resourceHolder = null;
        channelToUse = channel;
        bool boundHere = false;

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
            if (IsChannelLocallyTransacted && !TransactionSynchronizationManager.IsActualTransactionActive())
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
                var localResourceHolder = new RabbitResourceHolder(channelToUse, false, LoggerFactory?.CreateLogger<RabbitResourceHolder>())
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
            Logger?.LogDebug(exception, "Listener exception after container shutdown. Container: {name}", ServiceName);
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
            IEnumerable<IRabbitAdmin> admins = ApplicationContext.GetServices<IRabbitAdmin>();

            if (admins.Count() == 1)
            {
                RabbitAdmin = admins.Single();
            }
            else
            {
                if (AutoDeclare || MismatchedQueuesFatal)
                {
                    Logger?.LogDebug(
                        "For 'autoDeclare' and 'mismatchedQueuesFatal' to work, there must be exactly one " +
                        "RabbitAdmin in the context or you must inject one into this container; found: {count}" + " for container {container}", admins.Count(),
                        ToString());
                }

                if (MismatchedQueuesFatal)
                {
                    throw new InvalidOperationException("When 'mismatchedQueuesFatal' is 'true', there must be exactly " +
                        $"one RabbitAdmin in the context or you must inject one into this container; found: {admins.Count()} " +
                        $" for container {ToString()}");
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
                Logger?.LogInformation(e, "Broker not available; cannot check queue declarations. Container: {name}", ServiceName);
            }
            catch (RabbitIOException e)
            {
                if (RabbitUtils.IsMismatchedQueueArgs(e))
                {
                    throw new FatalListenerStartupException("Mismatched queues", e);
                }

                Logger?.LogInformation(e, "Failed to get connection during Start()");
            }
        }
        else
        {
            try
            {
                IConnection connection = ConnectionFactory.CreateConnection();

                if (connection != null)
                {
                    connection.Close();
                }
            }
            catch (Exception e)
            {
                Logger?.LogInformation(e, "Broker not available; cannot force queue declarations during start");
            }
        }
    }

    protected abstract void DoInitialize();

    protected abstract void DoShutdown();

    protected virtual void DoStart()
    {
        // Reschedule paused tasks, if any.
        lock (LifecycleMonitor)
        {
            IsActive = true;
            IsRunning = true;
            Monitor.PulseAll(LifecycleMonitor);
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
            throw new InvalidOperationException("You cannot acknowledge messages manually if the channel is not exposed to the listener " +
                "(please check your configuration and set exposeListenerChannel=true or " + "acknowledgeMode!=MANUAL)");
        }

        if (IsChannelTransacted && AcknowledgeMode.IsAutoAck())
        {
            throw new InvalidOperationException("The acknowledgeMode is NONE (autoack in Rabbit terms) which is not consistent with having a " +
                "transactional channel. Either use a different AcknowledgeMode or make sure " + "channelTransacted=false");
        }
    }

    protected virtual IRoutingConnectionFactory GetRoutingConnectionFactory()
    {
        return ConnectionFactory as IRoutingConnectionFactory;
    }

    protected virtual string GetRoutingLookupKey()
    {
        return ConnectionFactory is IRoutingConnectionFactory ? LookupKeyQualifier + GetQueuesAsListString() : null;
    }

    protected virtual void CheckMessageListener(object listener)
    {
        if (listener is not IMessageListener)
        {
            throw new ArgumentException($"Message listener needs to be of type [{nameof(IMessageListener)}] or [{nameof(IChannelAwareMessageListener)}]",
                nameof(listener));
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
        Exception cause = exception.InnerException;

        while (cause != null)
        {
            if (cause is ImmediateAcknowledgeException)
            {
                return true;
            }

            if (cause is RabbitRejectAndDoNotRequeueException)
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
            resourceHolder.RequeueOnRollback = AlwaysRequeueWithTxManagerRollback || ContainerUtils.ShouldRequeue(DefaultRequeueRejected, exception, Logger);
        }
    }

    private void AttemptDeclarations(IRabbitAdmin admin)
    {
        ISet<string> queueNames = GetQueueNamesAsSet();
        IEnumerable<IQueue> queueBeans = ApplicationContext.GetServices<IQueue>();

        foreach (IQueue entry in queueBeans)
        {
            if (MismatchedQueuesFatal || (queueNames.Contains(entry.QueueName) && admin.GetQueueProperties(entry.QueueName) == null))
            {
                Logger?.LogDebug("Redeclaring context exchanges, queues, bindings. Container: {name}", ServiceName);
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
                throw new FatalListenerExecutionException($"Illegal null id in {nameof(message)}. Failed to manage retry for: {message}", ex);
            }

            throw new ListenerExecutionFailedException("Cannot retry message more than once without an ID",
                new RabbitRejectAndDoNotRequeueException("Not retryable; rejecting and not requeuing", ex), message);
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
                RabbitUtils.CommitIfNecessary(channelToUse, Logger);
            }
        }
    }

    private void DoExecuteListener(R.IModel channel, IMessage message)
    {
        if (AfterReceivePostProcessors != null)
        {
            IMessage postProcessed = message;

            foreach (IMessagePostProcessor processor in AfterReceivePostProcessors)
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
        List<IQueue> queues = Queues;

        foreach (IQueue q in queues)
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
