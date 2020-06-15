// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Transaction;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Rabbit.Batch;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Data;
using Steeltoe.Messaging.Rabbit.Exceptions;
using Steeltoe.Messaging.Rabbit.Listener.Exceptions;
using Steeltoe.Messaging.Rabbit.Listener.Support;
using Steeltoe.Messaging.Rabbit.Support;
using Steeltoe.Messaging.Rabbit.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using R = RabbitMQ.Client;

namespace Steeltoe.Messaging.Rabbit.Listener
{
#pragma warning disable S3881 // "IDisposable" should be implemented correctly
    public abstract class AbstractMessageListenerContainer : RabbitAccessor, IMessageListenerContainer
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
    {
        public const int DEFAULT_FAILED_DECLARATION_RETRY_INTERVAL = 5000;
        public const long DEFAULT_SHUTDOWN_TIMEOUT = 5000;
        public const int DEFAULT_RECOVERY_INTERVAL = 5000;
        public const bool DEFAULT_DEBATCHING_ENABLED = true;
        public const int DEFAULT_PREFETCH_COUNT = 250;

        protected readonly object _consumersMonitor = new object();
        protected readonly object _lock = new object();
        protected readonly object _lifecycleMonitor = new object();
        protected readonly ILogger _logger;

        private string _listenerid;

        private List<Config.Queue> Queues { get; set; } = new List<Config.Queue>();

        protected AbstractMessageListenerContainer(IApplicationContext applicationContext, IConnectionFactory connectionFactory, string name = null, ILogger logger = null)
            : base(connectionFactory)
        {
            ApplicationContext = applicationContext;
            _logger = logger;
            ErrorHandler = new ConditionalRejectingErrorHandler(_logger);
            MessagePropertiesConverter = new DefaultMessagePropertiesConverter(_logger);
            ExclusiveConsumerExceptionLogger = new DefaultExclusiveConsumerLogger();
            BatchingStrategy = new SimpleBatchingStrategy(0, 0, 0L);
            TransactionAttribute = new DefaultTransactionAttribute();
            Name = name ?? GetType().Name + "@" + GetHashCode();
        }

        public IApplicationContext ApplicationContext { get; set; }

        public virtual AcknowledgeMode AcknowledgeMode { get; set; } = AcknowledgeMode.AUTO;

        public virtual string Name { get; set; }

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
            get
            {
                return _listenerid != null ? _listenerid : Name;
            }

            set
            {
                _listenerid = value;
            }
        }

        public virtual IConsumerTagStrategy ConsumerTagStrategy { get; set; }

        public virtual Dictionary<string, object> ConsumerArguments { get; set; } = new Dictionary<string, object>();

        public virtual bool Exclusive { get; set; }

        public virtual bool NoLocal { get; set; }

        public virtual bool DefaultRequeueRejected { get; set; } = true;

        public virtual int PrefetchCount { get; set; } = DEFAULT_PREFETCH_COUNT;

        public virtual long ShutdownTimeout { get; set; } = DEFAULT_SHUTDOWN_TIMEOUT;

        public virtual long IdleEventInterval { get; set; }

        public virtual long RecoveryInterval { get; set; }

        public IBackOff RecoveryBackOff { get; set; } = new FixedBackOff(DEFAULT_RECOVERY_INTERVAL, FixedBackOff.UNLIMITED_ATTEMPTS);

        public virtual IMessagePropertiesConverter MessagePropertiesConverter { get; set; }

        public virtual IAmqpAdmin AmqpAdmin { get; set; }

        public virtual bool MissingQueuesFatal { get; set; } = true;

        public virtual bool MismatchedQueuesFatal { get; set; } = false;

        public virtual bool PossibleAuthenticationFailureFatal { get; set; } = true;

        public virtual bool AutoDeclare { get; set; } = true;

        public virtual long FailedDeclarationRetryInterval { get; set; } = DEFAULT_FAILED_DECLARATION_RETRY_INTERVAL;

        public virtual bool StatefulRetryFatalWithNullMessageId { get; set; } = true;

        public virtual IConditionalExceptionLogger ExclusiveConsumerExceptionLogger { get; set; }

        public virtual bool AlwaysRequeueWithTxManagerRollback { get; set; }

        // Remove public string ErrorHandlerLoggerName { get; set; }
        public virtual IBatchingStrategy BatchingStrategy { get; set; }

        public virtual bool IsRunning { get; private set; } = false;

        public virtual bool IsActive { get; private set; } = false;

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

            var qs = new Config.Queue[queueNames.Length];
            var index = 0;
            foreach (var name in queueNames)
            {
                if (name == null)
                {
                    throw new ArgumentNullException("queue names cannot be null");
                }

                qs[index++] = new Config.Queue(name);
            }

            SetQueues(qs);
        }

        public virtual string[] GetQueueNames()
        {
            return QueuesToNames().ToArray();
        }

        public virtual void SetQueues(params Config.Queue[] queues)
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

                    if (string.IsNullOrEmpty(queue.Name))
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

            var qs = new Config.Queue[queueNames.Length];
            var index = 0;
            foreach (var name in queueNames)
            {
                if (name == null)
                {
                    throw new ArgumentNullException("queue names cannot be null");
                }

                qs[index++] = new Config.Queue(name);
            }

            AddQueues(qs);
        }

        public virtual void AddQueues(params Config.Queue[] queues)
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

                    if (string.IsNullOrEmpty(queue.Name))
                    {
                        throw new ArgumentException("Cannot add broker-named queues dynamically");
                    }
                }
            }

            var newQueues = new List<Config.Queue>(Queues);
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

            var copy = new List<Config.Queue>(Queues);
            var filtered = copy.Where((q) => !toRemove.Contains(q.ActualName)).ToList();
            Queues = filtered;
            return filtered.Count != copy.Count;
        }

        public virtual void RemoveQueues(params Config.Queue[] queues)
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

            var current = AfterReceivePostProcessors;
            if (current == null)
            {
                current = new List<IMessagePostProcessor>();
            }

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
            if (connectionFactory is IRoutingConnectionFactory)
            {
                var routingFactory = connectionFactory as IRoutingConnectionFactory;
                var targetConnectionFactory = routingFactory.GetTargetConnectionFactory(GetRoutingLookupKey()); // NOSONAR never null
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
                _logger?.LogError("Error initializing listener container", e);
                throw ConvertRabbitAccessException(e);
            }
        }

        public virtual void Dispose()
        {
            Shutdown();
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
                _logger?.LogError("DoShutdown erroro", ex);
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
                _logger?.LogDebug("Starting RabbitMQ listener container.");
                ConfigureAdminIfNeeded();
                CheckMismatchedQueues();
                DoStart();
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error Starting RabbitMQ listener container.", ex);
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
                _logger?.LogError("Error Stopping RabbitMQ listener container.", ex);
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

        protected virtual void RedeclareElementsIfNecessary()
        {
            lock (_lock)
            {
                var admin = AmqpAdmin;
                if (!IsLazyLoad && admin != null && AutoDeclare)
                {
                    try
                    {
                        AttemptDeclarations(admin);
                    }
                    catch (Exception e)
                    {
                        if (RabbitUtils.IsMismatchedQueueArgs(e))
                        {
                            throw new FatalListenerStartupException("Mismatched queues", e);
                        }

                        _logger?.LogError("Failed to check/redeclare auto-delete queue(s).", e);
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
                    _logger?.LogError("Execution of Rabbit message listener failed, and the error handler threw an exception", e);
                    throw;
                }
            }
            else
            {
                _logger?.LogWarning("Execution of Rabbit message listener failed, and no ErrorHandler has been set.", ex);
            }
        }

        protected virtual void ExecuteListener(R.IModel channel, object data)
        {
            if (!IsRunning)
            {
                _logger?.LogWarning("Rejecting received message(s) because the listener container has been stopped: " + data);
                throw new MessageRejectedWhileStoppingException();
            }

            try
            {
                DoExecuteListener(channel, data);
            }
            catch (Exception ex)
            {
                var message = data as Message;
                if (message == null && data is IList<Message> asList && asList.Count > 0)
                {
                    message = asList[0];
                }

                CheckStatefulRetry(ex, message);
                HandleListenerException(ex);
                throw;
            }
        }

        protected virtual void ActualInvokeListener(R.IModel channel, object data)
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
                    var resourceHolder = new RabbitResourceHolder(channel, false, _logger);
                    resourceHolder.SynchronizedWithTransaction = true;
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

        protected virtual void DoInvokeListener(IChannelAwareMessageListener listener, R.IModel channel, object data)
        {
            Message message = null;
            RabbitResourceHolder resourceHolder = null;
            var channelToUse = channel;
            var boundHere = false;
            try
            {
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
                        var localResourceHolder = new RabbitResourceHolder(channelToUse, false);
                        localResourceHolder.SynchronizedWithTransaction = true;
                        TransactionSynchronizationManager.BindResource(ConnectionFactory, localResourceHolder);
                        boundHere = true;
                    }
                }

                // Actually invoke the message listener...
                try
                {
                    if (data is List<Message> asList)
                    {
                        listener.OnMessageBatch(asList, channelToUse);
                    }
                    else
                    {
                        message = (Message)data;
                        listener.OnMessage(message, channelToUse);
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogError("Exception in OnMessage call", e);
                    throw WrapToListenerExecutionFailedExceptionIfNeeded(e, data);
                }
            }
            finally
            {
                CleanUpAfterInvoke(resourceHolder, channelToUse, boundHere);
            }
        }

        protected virtual void DoInvokeListener(IMessageListener listener, object data)
        {
            Message message = null;
            try
            {
                if (data is List<Message> asList)
                {
                    listener.OnMessageBatch(asList);
                }
                else
                {
                    message = (Message)data;
                    listener.OnMessage(message);
                }
            }
            catch (Exception e)
            {
                throw WrapToListenerExecutionFailedExceptionIfNeeded(e, data);
            }
        }

        protected virtual ListenerExecutionFailedException WrapToListenerExecutionFailedExceptionIfNeeded(Exception exception, object data)
        {
            if (!(exception is ListenerExecutionFailedException listnerExcep))
            {
                // Wrap exception to ListenerExecutionFailedException.
                if (data is List<Message> asList)
                {
                    return new ListenerExecutionFailedException("Listener threw exception", exception, asList.ToArray());
                }
                else
                {
                    return new ListenerExecutionFailedException("Listener threw exception", exception, (Message)data);
                }
            }

            return (ListenerExecutionFailedException)exception;
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
                _logger?.LogDebug("Listener exception after container shutdown", exception);
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
            if (AmqpAdmin == null)
            {
                var admins = ApplicationContext.GetServices<IAmqpAdmin>();
                if (admins.Count() == 1)
                {
                    AmqpAdmin = admins.Single();
                }
                else
                {
                    if (AutoDeclare || MismatchedQueuesFatal)
                    {
                        _logger?.LogDebug("For 'autoDeclare' and 'mismatchedQueuesFatal' to work, there must be exactly one "
                                + "AmqpAdmin in the context or you must inject one into this container; found: "
                                + admins.Count() + " for container " + ToString());
                    }

                    if (MismatchedQueuesFatal)
                    {
                        throw new InvalidOperationException("When 'mismatchedQueuesFatal' is 'true', there must be exactly "
                                + "one AmqpAdmin in the context or you must inject one into this container; found: "
                                + admins.Count() + " for container " + ToString());
                    }
                }
            }
        }

        protected virtual void CheckMismatchedQueues()
        {
            if (MismatchedQueuesFatal && AmqpAdmin != null)
            {
                try
                {
                    AmqpAdmin.Initialize();
                }
                catch (AmqpConnectException e)
                {
                    _logger?.LogInformation("Broker not available; cannot check queue declarations", e);
                }
                catch (AmqpIOException e)
                {
                    if (RabbitUtils.IsMismatchedQueueArgs(e))
                    {
                        throw new FatalListenerStartupException("Mismatched queues", e);
                    }
                    else
                    {
                        _logger?.LogInformation("Failed to get connection during start(): ", e);
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
                    _logger?.LogInformation("Broker not available; cannot force queue declarations during start: " + e.Message);
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
            return GetConnectionFactory() is IRoutingConnectionFactory ? (IRoutingConnectionFactory)ConnectionFactory : null;
        }

        protected virtual long LastReceive { get; private set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        protected virtual bool ForceCloseChannel { get; set; } = true;

        protected virtual string GetRoutingLookupKey()
        {
            return ConnectionFactory is IRoutingConnectionFactory ? LookupKeyQualifier + GetQueuesAsListString() : null;
        }

        protected virtual void CheckMessageListener(object listener)
        {
            if (!(listener is IMessageListener))
            {
                throw new ArgumentException(
                    "Message listener needs to be of type [" + typeof(IMessageListener).Name + "] or [" + typeof(IChannelAwareMessageListener).Name + "]");
            }
        }

        protected virtual ISet<string> GetQueueNamesAsSet()
        {
            return new HashSet<string>(QueuesToNames());
        }

        protected virtual Dictionary<string, Config.Queue> GetQueueNamesToQueues()
        {
            return Queues.ToDictionary((q) => q.ActualName);
        }

        protected virtual bool CauseChainHasImmediateAcknowledgeAmqpException(Exception exception)
        {
            // if (ex instanceof Error) {
            //    return false;
            // }
            var cause = exception.InnerException;
            while (cause != null)
            {
                if (cause is ImmediateAcknowledgeAmqpException)
                {
                    return true;
                }
                else if (cause is AmqpRejectAndDontRequeueException)
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

        private void AttemptDeclarations(IAmqpAdmin admin)
        {
            var queueNames = GetQueueNamesAsSet();
            var queueBeans = ApplicationContext.GetServices<Config.Queue>();
            foreach (var entry in queueBeans)
            {
                if (MismatchedQueuesFatal || (queueNames.Contains(entry.Name) && admin.GetQueueProperties(entry.Name) == null))
                {
                    _logger?.LogDebug("Redeclaring context exchanges, queues, bindings.");
                    admin.Initialize();
                    break;
                }
            }
        }

        private void CheckStatefulRetry(Exception ex, Message message)
        {
            if (message.MessageProperties.IsFinalRetryForMessageWithNoId)
            {
                if (StatefulRetryFatalWithNullMessageId)
                {
                    throw new FatalListenerExecutionException("Illegal null id in message. Failed to manage retry for message: " + message, ex);
                }
                else
                {
                    throw new ListenerExecutionFailedException(
                        "Cannot retry message more than once without an ID",
                        new AmqpRejectAndDontRequeueException("Not retryable; rejecting and not requeuing", ex),
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

            ConnectionFactoryUtils.ReleaseResources(resourceHolder); // NOSONAR - null check in method
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

        private void DoExecuteListener(R.IModel channel, object data)
        {
            if (data is Message asMessage)
            {
                if (AfterReceivePostProcessors != null)
                {
                    foreach (var processor in AfterReceivePostProcessors)
                    {
                        asMessage = processor.PostProcessMessage(asMessage);
                        if (asMessage == null)
                        {
                            throw new ImmediateAcknowledgeAmqpException("Message Post Processor returned 'null', discarding message");
                        }
                    }
                }

                if (IsDeBatchingEnabled && BatchingStrategy.CanDebatch(asMessage.MessageProperties))
                {
                    BatchingStrategy.DeBatch(asMessage, (fragment) => ActualInvokeListener(channel, fragment));
                }
                else
                {
                    ActualInvokeListener(channel, asMessage);
                }
            }
            else
            {
                ActualInvokeListener(channel, data);
            }
        }

        private string GetQueuesAsListString()
        {
            var sb = new StringBuilder("[");
            var queues = Queues;
            foreach (var q in queues)
            {
                sb.Append(q.Name);
                sb.Append(",");
            }

            return sb.ToString(0, sb.Length - 1) + "]";
        }

        private List<string> QueuesToNames()
        {
            return Queues.Select((q) => q.Name).ToList();
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

        private class DefaultExclusiveConsumerLogger : IConditionalExceptionLogger
        {
            public void Log(ILogger logger, string message, object cause)
            {
                logger.LogError("Unexpected invocation of " + GetType() + ", with message: " + message, cause);
            }
        }
    }
}
