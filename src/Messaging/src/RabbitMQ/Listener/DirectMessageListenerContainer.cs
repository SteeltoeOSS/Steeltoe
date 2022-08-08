// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Transaction;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Listener.Support;
using Steeltoe.Messaging.RabbitMQ.Support;
using Steeltoe.Messaging.RabbitMQ.Transaction;
using Steeltoe.Messaging.RabbitMQ.Util;
using Steeltoe.Messaging.Support;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

public class DirectMessageListenerContainer : AbstractMessageListenerContainer
{
    protected const int StartWaitTime = 60;
    protected const int DefaultMonitorInterval = 10_000;
    protected const int DefaultAckTimeout = 20_000;

    protected internal readonly List<SimpleConsumer> Consumers = new();
    protected internal readonly Dictionary<string, List<SimpleConsumer>> ConsumersByQueue = new();
    protected internal readonly ActiveObjectCounter<SimpleConsumer> CancellationLock = new();
    protected internal readonly List<SimpleConsumer> ConsumersToRestart = new();

    private int _consumersPerQueue = 1;
    private volatile bool _started;
    private volatile bool _aborted;
    private volatile bool _hasStopped;
    private long _lastAlertAt;
    private Task _consumerMonitorTask;
    private CancellationTokenSource _consumerMonitorCancellationToken;
    internal CountdownEvent StartedLatch = new(1);

    public virtual int ConsumersPerQueue
    {
        get => _consumersPerQueue;

        set
        {
            if (IsRunning)
            {
                AdjustConsumers(value);
            }

            _consumersPerQueue = value;
        }
    }

    public override bool Exclusive
    {
        get => base.Exclusive;

        set
        {
            if (value && ConsumersPerQueue != 1)
            {
                throw new ArgumentException("When the consumer is exclusive, the consumers per queue must be 1");
            }

            base.Exclusive = value;
        }
    }

    public virtual long MonitorInterval { get; set; } = DefaultMonitorInterval;

    public virtual int MessagesPerAck { get; set; }

    public virtual long AckTimeout { get; set; } = DefaultAckTimeout;

    public virtual long LastRestartAttempt { get; private set; }

    public DirectMessageListenerContainer(string name = null, ILoggerFactory loggerFactory = null)
        : this(null, null, name, loggerFactory)
    {
    }

    public DirectMessageListenerContainer(IApplicationContext applicationContext, string name = null, ILoggerFactory loggerFactory = null)
        : this(applicationContext, null, name, loggerFactory)
    {
    }

    public DirectMessageListenerContainer(IApplicationContext applicationContext, IConnectionFactory connectionFactory, string name = null,
        ILoggerFactory loggerFactory = null)
        : base(applicationContext, connectionFactory, name, loggerFactory)
    {
        MissingQueuesFatal = false;
    }

    public override void SetQueueNames(params string[] queueNames)
    {
        RemoveQueues(queueNames.AsEnumerable());
        base.RemoveQueueNames(queueNames);
        base.SetQueueNames(queueNames);
        UpdateQueues();
    }

    public override void AddQueueNames(params string[] queueNames)
    {
        if (queueNames == null)
        {
            throw new ArgumentNullException(nameof(queueNames));
        }

        try
        {
            IEnumerable<string> names = queueNames.Select(n => n ?? throw new ArgumentNullException("queue names cannot be null"));
            AddQueues(names);
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "Failed to add queue names");
            throw;
        }

        base.AddQueueNames(queueNames);
    }

    public override void AddQueues(params IQueue[] queues)
    {
        if (queues == null)
        {
            throw new ArgumentNullException(nameof(queues));
        }

        try
        {
            IEnumerable<string> names = queues.Select(q => q != null ? q.QueueName : throw new ArgumentNullException("queues cannot contain nulls"));
            AddQueues(names);
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "Failed to add queues");
            throw;
        }

        base.AddQueues(queues);
    }

    public override bool RemoveQueueNames(params string[] queueNames)
    {
        RemoveQueues(queueNames.AsEnumerable());
        return base.RemoveQueueNames(queueNames);
    }

    public override void RemoveQueues(params IQueue[] queues)
    {
        RemoveQueues(queues.Select(q => q.ActualName));
        base.RemoveQueues(queues);
    }

    protected virtual int FindIdleConsumer()
    {
        return 0;
    }

    protected override void DoInitialize()
    {
        if (MessagesPerAck > 0 && IsChannelTransacted)
        {
            throw new ArgumentException("'messagesPerAck' is not allowed with transactions");
        }
    }

    protected override void DoStart()
    {
        if (!_started)
        {
            ActualStart();
        }
    }

    protected virtual void ActualStart()
    {
        _aborted = false;
        _hasStopped = false;

        if (PrefetchCount < MessagesPerAck)
        {
            PrefetchCount = MessagesPerAck;
        }

        base.DoStart();
        CheckListenerContainerAware();
        string[] queueNames = GetQueueNames();
        CheckMissingQueues(queueNames);

        if (IdleEventInterval > 0 && MonitorInterval > IdleEventInterval)
        {
            MonitorInterval = IdleEventInterval / 2;
        }

        if (FailedDeclarationRetryInterval < MonitorInterval)
        {
            MonitorInterval = FailedDeclarationRetryInterval;
        }

        Dictionary<string, IQueue> namesToQueues = GetQueueNamesToQueues();
        LastRestartAttempt = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        StartMonitor(IdleEventInterval, namesToQueues);

        if (queueNames.Length > 0)
        {
            DoRedeclareElementsIfNecessary();
            Task.Run(() => StartConsumers(queueNames));
        }
        else
        {
            _started = true;
            StartedLatch.Signal();
        }

        Logger?.LogInformation("Container initialized for queues: {queues}", string.Join(',', queueNames));
    }

    protected virtual void DoRedeclareElementsIfNecessary()
    {
        string routingLookupKey = GetRoutingLookupKey();

        if (routingLookupKey != null)
        {
            SimpleResourceHolder.Push(GetRoutingConnectionFactory(), routingLookupKey);
        }

        try
        {
            RedeclareElementsIfNecessary();
        }
        catch (FatalListenerStartupException fe)
        {
            Logger?.LogError(fe, "Fatal exception while redeclare elements");
            throw;
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "Failed to redeclare elements");
        }
        finally
        {
            if (routingLookupKey != null)
            {
                SimpleResourceHolder.Pop(GetRoutingConnectionFactory());
            }
        }
    }

    protected override void DoShutdown()
    {
        List<SimpleConsumer> canceledConsumers = null;
        bool waitForConsumers = false;

        lock (ConsumersMonitor)
        {
            if (_started || _aborted)
            {
                // Copy in the same order to avoid ConcurrentModificationException during remove in the
                // cancelConsumer().
                canceledConsumers = new List<SimpleConsumer>(Consumers);
                ActualShutDown(canceledConsumers);
                waitForConsumers = true;
            }
        }

        if (waitForConsumers)
        {
            try
            {
                if (CancellationLock.Wait(TimeSpan.FromMilliseconds(ShutdownTimeout)))
                {
                    Logger?.LogInformation("Successfully waited for consumers to finish.");
                }
                else
                {
                    Logger?.LogInformation("Consumers not finished.");

                    if (ForceCloseChannel)
                    {
                        canceledConsumers.ForEach(consumer =>
                        {
                            string eventMessage = $"Closing channel for unresponsive consumer: {consumer} ";
                            Logger?.LogWarning(eventMessage);
                            consumer.CancelConsumer(eventMessage);
                        });
                    }
                }
            }
            catch (Exception e)
            {
                // Thread.currentThread().interrupt();
                Logger?.LogWarning(e, "Interrupted waiting for consumers. Continuing with shutdown.");
            }
            finally
            {
                StartedLatch = new CountdownEvent(1);
                _started = false;
                _aborted = false;
                _hasStopped = true;
            }
        }
    }

    protected virtual void ProcessMonitorTask()
    {
        // Override if needed
    }

    protected virtual void ConsumerRemoved(SimpleConsumer consumer)
    {
        // Override if needed
    }

    private void CheckListenerContainerAware()
    {
        if (MessageListener is IListenerContainerAware listenerAware)
        {
            List<string> expectedQueueNames = listenerAware.GetExpectedQueueNames();
            string[] queueNames = GetQueueNames();

            if (expectedQueueNames.Count != queueNames.Length)
            {
                throw new InvalidOperationException("Listener expects queues that the container is not listening on");
            }

            if (!queueNames.Any(name => expectedQueueNames.Contains(name)))
            {
                throw new InvalidOperationException("Listener expects queues that the container is not listening on");
            }
        }
    }

    private void CheckMissingQueues(string[] queueNames)
    {
        if (MissingQueuesFatal)
        {
            IRabbitAdmin checkAdmin = RabbitAdmin;

            if (checkAdmin == null)
            {
                checkAdmin = new RabbitAdmin(ApplicationContext, ConnectionFactory, LoggerFactory?.CreateLogger<RabbitAdmin>());
                RabbitAdmin = checkAdmin;
            }

            foreach (string queue in queueNames)
            {
                Dictionary<string, object> queueProperties = checkAdmin.GetQueueProperties(queue);

                if (queueProperties == null && MissingQueuesFatal)
                {
                    throw new InvalidOperationException("At least one of the configured queues is missing");
                }
            }
        }
    }

    private void ActualShutDown(List<SimpleConsumer> consumers)
    {
        // Assert.state(getTaskExecutor() != null, "Cannot shut down if not initialized");
        Logger?.LogDebug("Shutting down");
        consumers.ForEach(CancelConsumer);
        Consumers.Clear();
        ConsumersByQueue.Clear();
        Logger?.LogDebug("All consumers canceled");

        if (_consumerMonitorTask != null)
        {
            _consumerMonitorCancellationToken.Cancel();
            _consumerMonitorTask = null;
        }
    }

    private void ConsumeFromQueue(string queue)
    {
        ConsumersByQueue.TryGetValue(queue, out List<SimpleConsumer> list);

        // Possible race with setConsumersPerQueue and the task launched by start()
        if (list == null || list.Count == 0)
        {
            for (int i = 0; i < _consumersPerQueue; i++)
            {
                DoConsumeFromQueue(queue);
            }
        }
    }

    private void DoConsumeFromQueue(string queue)
    {
        if (!IsActive)
        {
            Logger?.LogDebug("Consume from queue {queueName} ignore, container stopping", queue);
            return;
        }

        string routingLookupKey = GetRoutingLookupKey();

        if (routingLookupKey != null)
        {
            SimpleResourceHolder.Push(GetRoutingConnectionFactory(), routingLookupKey);
        }

        IConnection connection = null;

        try
        {
            connection = ConnectionFactory.CreateConnection();
        }
        catch (Exception e)
        {
            // publishConsumerFailedEvent(e.getMessage(), false, e);
            Logger?.LogError(e, "Exception while CreateConnection");
            AddConsumerToRestart(new SimpleConsumer(this, null, null, queue, LoggerFactory?.CreateLogger<SimpleConsumer>()));
            throw;

            // throw e instanceof AmqpConnectException
            // ? (AmqpConnectException)e
            // : new AmqpConnectException(e);
        }
        finally
        {
            if (routingLookupKey != null)
            {
                SimpleResourceHolder.Pop(GetRoutingConnectionFactory());
            }
        }

        SimpleConsumer consumer = Consume(queue, connection);

        lock (ConsumersMonitor)
        {
            if (consumer != null)
            {
                CancellationLock.Add(consumer);
                Consumers.Add(consumer);
                ConsumersByQueue.TryGetValue(queue, out List<SimpleConsumer> list);

                if (list == null)
                {
                    list = new List<SimpleConsumer>();
                    ConsumersByQueue.Add(queue, list);
                }

                list.Add(consumer);
                Logger?.LogInformation("{consumer} started", consumer);

                // if (getApplicationEventPublisher() != null)
                // {
                //    getApplicationEventPublisher().publishEvent(new AsyncConsumerStartedEvent(this, consumer));
                // }
            }
        }
    }

    private SimpleConsumer Consume(string queue, IConnection connection)
    {
        RC.IModel channel = null;
        SimpleConsumer consumer = null;

        try
        {
            channel = connection.CreateChannel(IsChannelTransacted);
            channel.BasicQos(0, (ushort)PrefetchCount, false);
            consumer = new SimpleConsumer(this, connection, channel, queue, LoggerFactory?.CreateLogger<SimpleConsumer>());
            channel.QueueDeclarePassive(queue);

            consumer.ConsumerTag = channel.BasicConsume(queue, AcknowledgeMode.IsAutoAck(),
                ConsumerTagStrategy != null ? ConsumerTagStrategy.CreateConsumerTag(queue) : string.Empty, NoLocal, Exclusive, ConsumerArguments, consumer);
        }
        catch (Exception e)
        {
            RabbitUtils.CloseChannel(channel, Logger);
            RabbitUtils.CloseConnection(connection, Logger);

            consumer = HandleConsumeException(queue, consumer, e);
        }

        return consumer;
    }

    private SimpleConsumer HandleConsumeException(string queue, SimpleConsumer consumerArg, Exception e)
    {
        SimpleConsumer consumer = consumerArg;

        Logger?.LogWarning(e, $"basicConsume failed, scheduling consumer {consumer?.ToString() ?? $"for queue {queue}"} for restart");

        if (consumer == null)
        {
            AddConsumerToRestart(new SimpleConsumer(this, null, null, queue, LoggerFactory?.CreateLogger<SimpleConsumer>()));
        }
        else
        {
            AddConsumerToRestart(consumer);
            consumer = null;
        }

        return consumer;
    }

    private void StartMonitor(long idleEventInterval, Dictionary<string, IQueue> namesToQueues)
    {
        _consumerMonitorCancellationToken = new CancellationTokenSource();

        _consumerMonitorTask = Task.Run(async () =>
        {
            bool shouldShutdown = false;

            while (!_consumerMonitorCancellationToken.Token.IsCancellationRequested && !shouldShutdown)
            {
                long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                CheckIdle(idleEventInterval, now);
                CheckConsumers(now);

                if (LastRestartAttempt + FailedDeclarationRetryInterval < now)
                {
                    lock (ConsumersMonitor)
                    {
                        var restartableConsumers = new List<SimpleConsumer>(ConsumersToRestart);
                        ConsumersToRestart.Clear();

                        if (_started)
                        {
                            if (restartableConsumers.Count > 0)
                            {
                                try
                                {
                                    DoRedeclareElementsIfNecessary();
                                }
                                catch (FatalListenerStartupException)
                                {
                                    shouldShutdown = true;
                                }
                            }

                            if (!shouldShutdown)
                            {
                                foreach (SimpleConsumer consumer in restartableConsumers)
                                {
                                    if (!ConsumersByQueue.ContainsKey(consumer.Queue))
                                    {
                                        Logger?.LogDebug("Skipping restart of consumer {consumer} ", consumer);
                                        continue;
                                    }

                                    Logger?.LogDebug("Attempting to restart consumer {consumer}", consumer);

                                    if (!RestartConsumer(namesToQueues, restartableConsumers, consumer))
                                    {
                                        break;
                                    }
                                }

                                LastRestartAttempt = now;
                            }
                        }
                    }
                }

                ProcessMonitorTask();

                if (shouldShutdown)
                {
                    Shutdown();
                }
                else
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(MonitorInterval), _consumerMonitorCancellationToken.Token);
                }
            }
        }, _consumerMonitorCancellationToken.Token);
    }

    private void CheckIdle(long idleEventInterval, long now)
    {
        if (idleEventInterval > 0 && now - LastReceive > idleEventInterval && now - _lastAlertAt > idleEventInterval)
        {
            // PublishIdleContainerEvent(now - LastReceive);
            _lastAlertAt = now;
        }
    }

    private void CancelConsumer(SimpleConsumer consumer)
    {
        try
        {
            Logger?.LogDebug("Canceling {consumer}", consumer);

            lock (consumer)
            {
                consumer.Canceled = true;

                if (MessagesPerAck > 1)
                {
                    try
                    {
                        consumer.AckIfNecessary(0L);
                    }
                    catch (Exception e)
                    {
                        Logger?.LogError(e, "Exception while sending delayed ack");
                    }
                }
            }

            RabbitUtils.Cancel(consumer.Model, consumer.ConsumerTag, Logger);
        }
        finally
        {
            Consumers.Remove(consumer);
            ConsumerRemoved(consumer);
        }
    }

    private void CheckConsumers(long now)
    {
        List<SimpleConsumer> consumersToCancel;

        lock (ConsumersMonitor)
        {
            consumersToCancel = Consumers.Where(consumer =>
            {
                bool open = consumer.Model.IsOpen && !consumer.AckFailed && !consumer.TargetChanged;

                if (open && MessagesPerAck > 1)
                {
                    try
                    {
                        consumer.AckIfNecessary(now);
                    }
                    catch (Exception e)
                    {
                        Logger?.LogError(e, "Exception while sending delayed ack");
                    }
                }

                if (!open)
                {
                    return !open;
                }

                return !open;
            }).ToList();
        }

        consumersToCancel.ForEach(consumer =>
        {
            try
            {
                RabbitUtils.CloseMessageConsumer(consumer.Model, new List<string>
                {
                    consumer.ConsumerTag
                }, IsChannelTransacted, Logger);
            }
            catch (Exception e)
            {
                Logger?.LogDebug(e, "Error closing consumer {consumer} ", consumer);
            }

            Logger?.LogError("Consumer {consumer} canceled - channel closed ", consumer);
            consumer.CancelConsumer($"Consumer {consumer} channel closed");
        });
    }

    private bool RestartConsumer(Dictionary<string, IQueue> namesToQueues, List<SimpleConsumer> restartableConsumers, SimpleConsumer consumerArg)
    {
        SimpleConsumer consumer = consumerArg;
        namesToQueues.TryGetValue(consumer.Queue, out IQueue queue);

        if (queue != null && string.IsNullOrEmpty(queue.QueueName))
        {
            // check to see if a broker-declared queue name has changed
            string actualName = queue.ActualName;

            if (!string.IsNullOrEmpty(actualName))
            {
                namesToQueues.Remove(consumer.Queue);
                namesToQueues[actualName] = queue;
                consumer = new SimpleConsumer(this, null, null, actualName, LoggerFactory?.CreateLogger<SimpleConsumer>());
            }
        }

        try
        {
            DoConsumeFromQueue(consumer.Queue);
            return true;
        }
        catch (Exception e)
        {
            Logger?.LogError(e, "Cannot connect to server");

            // if (e.getCause() instanceof AmqpApplicationContextClosedException) {
            //    this.logger.error("Application context is closed, terminating");
            //    this.taskScheduler.schedule(this::stop, new Date());
            // }
            ConsumersToRestart.AddRange(restartableConsumers);
            Logger?.LogTrace("After restart exception, consumers to restart now: {consumersToRestart}", ConsumersToRestart.Count);
            return false;
        }
    }

    private void StartConsumers(string[] queueNames)
    {
        lock (ConsumersMonitor)
        {
            if (_hasStopped)
            {
                // container stopped before we got the lock
                Logger?.LogDebug("Consumer start aborted - container stopping");
            }
            else
            {
                IBackOffExecution backOffExecution = RecoveryBackOff.Start();

                while (!_started && IsRunning)
                {
                    CancellationLock.Reset();

                    try
                    {
                        foreach (string queue in queueNames)
                        {
                            ConsumeFromQueue(queue);
                        }
                    }
                    catch (Exception e) when (e is RabbitConnectException || e is RabbitIOException)
                    {
                        int nextBackOff = backOffExecution.NextBackOff();

                        if (nextBackOff < 0)
                        {
                            _aborted = true;
                            Shutdown();
                            Logger?.LogError(e, "Failed to start container - fatal error or backOffs exhausted");
                            Task.Run(StopAsync);

                            // this.taskScheduler.schedule(this::stop, new Date());
                            break;
                        }

                        Logger?.LogError(e, "Error creating consumer; retrying in {nextBackOff}", nextBackOff);
                        DoShutdown();

                        try
                        {
                            Thread.Sleep(nextBackOff);
                        }
                        catch (Exception e1)
                        {
                            Logger?.LogError(e1, "Exception while in backoff, {nextBackOff}", nextBackOff);

                            // Thread.currentThread().interrupt();
                        }

                        // initialization failed; try again having rested for backOff-interval
                        continue;
                    }

                    _started = true;
                    StartedLatch.Signal();
                }
            }
        }
    }

    private void CheckStartState()
    {
        if (!IsRunning)
        {
            try
            {
                if (!StartedLatch.Wait(TimeSpan.FromSeconds(StartWaitTime)))
                {
                    throw new InvalidOperationException("Container is not started - cannot adjust queues");
                }
            }
            catch (Exception e)
            {
                Logger?.LogError(e, "Exception thrown while waiting for container start");
                throw;
            }
        }
    }

    private void UpdateQueues()
    {
        if (IsRunning)
        {
            lock (ConsumersMonitor)
            {
                CheckStartState();
                ISet<string> current = GetQueueNamesAsSet();

                foreach (string name in current)
                {
                    ConsumeFromQueue(name);
                }
            }
        }
    }

    private void AddQueues(IEnumerable<string> names)
    {
        if (IsRunning)
        {
            lock (ConsumersMonitor)
            {
                CheckStartState();
                ISet<string> current = GetQueueNamesAsSet();

                foreach (string name in names)
                {
                    if (current.Contains(name))
                    {
                        Logger?.LogWarning("Queue " + name + " is already configured for this container: " + this + ", ignoring add");
                    }
                    else
                    {
                        ConsumeFromQueue(name);
                    }
                }
            }
        }
    }

    private void RemoveQueues(IEnumerable<string> queueNames)
    {
        if (IsRunning)
        {
            lock (ConsumersMonitor)
            {
                CheckStartState();

                foreach (string name in queueNames)
                {
                    if (name != null && ConsumersByQueue.TryGetValue(name, out List<SimpleConsumer> consumers))
                    {
                        foreach (SimpleConsumer consumer in consumers)
                        {
                            CancelConsumer(consumer);
                        }
                    }
                }
            }
        }
    }

    private void AdjustConsumers(int newCount)
    {
        lock (ConsumersMonitor)
        {
            CheckStartState();
            ConsumersToRestart.Clear();

            foreach (string queue in GetQueueNames())
            {
                ConsumersByQueue.TryGetValue(queue, out List<SimpleConsumer> consumers);

                while (consumers == null || consumers.Count < newCount)
                {
                    DoConsumeFromQueue(queue);
                    ConsumersByQueue.TryGetValue(queue, out consumers);
                }

                if (consumers.Count > newCount)
                {
                    int delta = consumers.Count - newCount;

                    for (int i = 0; i < delta; i++)
                    {
                        int index = FindIdleConsumer();

                        if (index >= 0)
                        {
                            SimpleConsumer consumer = consumers[index];
                            consumers.RemoveAt(index);

                            if (consumer != null)
                            {
                                CancelConsumer(consumer);
                            }
                        }
                    }
                }
            }
        }
    }

    private void AddConsumerToRestart(SimpleConsumer consumer)
    {
        if (_started)
        {
            ConsumersToRestart.Add(consumer);
            Logger?.LogTrace("Consumers to restart now: {count}", ConsumersToRestart.Count);
        }
    }

    protected internal sealed class SimpleConsumer : RC.DefaultBasicConsumer
    {
        private readonly DirectMessageListenerContainer _container;
        private readonly IConnection _connection;
        private readonly RC.IModel _targetChannel;
        private readonly ILogger _logger;
        private readonly object _lock = new();

        public string Queue { get; internal set; }

        public int Epoch { get; internal set; }

        public bool Canceled { get; internal set; }

        public bool AckFailed { get; internal set; }

        public bool AckRequired { get; internal set; }

        public int MessagesPerAck { get; internal set; }

        public IPlatformTransactionManager TransactionManager { get; internal set; }

        public ITransactionAttribute TransactionAttribute { get; internal set; }

        public TransactionTemplate TransactionTemplate { get; internal set; }

        public IConnectionFactory ConnectionFactory { get; internal set; }

        public bool IsRabbitTxManager { get; internal set; }

        public ulong LatestDeferredDeliveryTag { get; internal set; }

        public int PendingAcks { get; internal set; }

        public long LastAck { get; internal set; }

        public long AckTimeout { get; internal set; }

        public bool TargetChanged => _targetChannel != null && !_targetChannel.Equals(((IChannelProxy)Model).TargetChannel);

        public SimpleConsumer(DirectMessageListenerContainer container, IConnection connection, RC.IModel channel, string queue, ILogger logger = null)
            : base(channel)
        {
            _container = container;
            _connection = connection;
            Queue = queue;
            AckRequired = !_container.AcknowledgeMode.IsAutoAck() && !_container.AcknowledgeMode.IsManual();
            _targetChannel = channel is IChannelProxy proxy ? proxy.TargetChannel : null;

            _logger = logger;
            TransactionManager = _container.TransactionManager;
            TransactionAttribute = _container.TransactionAttribute;
            IsRabbitTxManager = TransactionManager is RabbitTransactionManager;
            ConnectionFactory = _container.ConnectionFactory;
            MessagesPerAck = _container.MessagesPerAck;
            LastAck = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            AckTimeout = _container.AckTimeout;
        }

        public int IncrementAndGetEpoch()
        {
            Epoch++;
            return Epoch;
        }

        public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey,
            RC.IBasicProperties properties, byte[] body)
        {
            var envelope = new Envelope(deliveryTag, redelivered, exchange, routingKey);
            IMessageHeaders messageHeaders = _container.MessageHeadersConverter.ToMessageHeaders(properties, envelope, EncodingUtils.Utf8);
            var headerAccessor = MessageHeaderAccessor.GetAccessor<RabbitHeaderAccessor>(messageHeaders);
            headerAccessor.ConsumerTag = consumerTag;
            headerAccessor.ConsumerQueue = Queue;
            IMessage<byte[]> message = Message.Create(body, headerAccessor.MessageHeaders);
            _logger?.LogDebug("Received {message} {consumer}", message, this);
            _container.UpdateLastReceive();

            if (TransactionManager != null)
            {
                try
                {
                    ExecuteListenerInTransaction(message, deliveryTag);
                }

                // catch (WrappedTransactionException e)
                // {
                //    if (e.InnerException instanceof Error) {
                //        throw (Error)e.getCause();
                //    }
                // }
                catch (Exception)
                {
                    // empty
                }
                finally
                {
                    if (IsRabbitTxManager)
                    {
                        ConsumerChannelRegistry.UnRegisterConsumerChannel();
                    }
                }
            }
            else
            {
                try
                {
                    CallExecuteListener(message, deliveryTag);
                }
                catch (Exception)
                {
                    // Intentionally left empty.
                }
            }
        }

        public override void HandleBasicConsumeOk(string consumerTag)
        {
            base.HandleBasicConsumeOk(consumerTag);
            _logger?.LogDebug("New {consumer} consumeOk", this);
        }

        public override void HandleBasicCancelOk(string consumerTag)
        {
            _logger?.LogDebug("CancelOk {consumer}", this);
            FinalizeConsumer();
        }

        public override void HandleBasicCancel(string consumerTag)
        {
            _logger?.LogError("Consumer canceled - queue deleted? {consumerTag}, {consumer}", consumerTag, this);
            CancelConsumer($"Consumer {this} canceled");
        }

        public override string ToString()
        {
            return $"SimpleConsumer [queue={Queue}, consumerTag={ConsumerTag} identity={GetHashCode()}]";
        }

        internal void CancelConsumer(string eventMessage)
        {
            lock (_container.ConsumersMonitor)
            {
                if (_container.ConsumersByQueue.TryGetValue(Queue, out List<SimpleConsumer> list))
                {
                    list.Remove(this);
                }

                _container.Consumers.Remove(this);
                _container.AddConsumerToRestart(this);
            }

            FinalizeConsumer();
        }

        internal void AckIfNecessary(long now)
        {
            lock (_lock)
            {
                if (PendingAcks >= MessagesPerAck || (PendingAcks > 0 && (now - LastAck > AckTimeout || Canceled)))
                {
                    SendAck(now);
                }
            }
        }

        internal void SendAck(long now)
        {
            lock (_lock)
            {
                Model.BasicAck(LatestDeferredDeliveryTag, true);
                LastAck = now;
                PendingAcks = 0;
            }
        }

        private void ExecuteListenerInTransaction(IMessage<byte[]> message, ulong deliveryTag)
        {
            if (IsRabbitTxManager)
            {
                ConsumerChannelRegistry.RegisterConsumerChannel(Model, ConnectionFactory);
            }

            TransactionTemplate ??= new TransactionTemplate(TransactionManager, TransactionAttribute, _logger);

            TransactionTemplate.Execute<object>(_ =>
            {
                RabbitResourceHolder resourceHolder = ConnectionFactoryUtils.BindResourceToTransaction(
                    new RabbitResourceHolder(Model, false, _container.LoggerFactory?.CreateLogger<RabbitResourceHolder>()), ConnectionFactory, true);

                if (resourceHolder != null)
                {
                    resourceHolder.AddDeliveryTag(Model, deliveryTag);
                }

                // unbound in ResourceHolderSynchronization.beforeCompletion()
                try
                {
                    CallExecuteListener(message, deliveryTag);
                }
                catch (Exception e1)
                {
                    _container.PrepareHolderForRollback(resourceHolder, e1);
                    throw;
                }

                // catch (Throwable e2)
                // {
                //    throw new WrappedTransactionException(e2);
                // }
                return null;
            });
        }

        private void CallExecuteListener(IMessage<byte[]> message, ulong deliveryTag)
        {
            bool channelLocallyTransacted = _container.IsChannelLocallyTransacted;

            try
            {
                _container.ExecuteListener(Model, message);
                HandleAck(deliveryTag, channelLocallyTransacted);
            }
            catch (ImmediateAcknowledgeException e)
            {
                _logger?.LogDebug(e, "User requested ack for failed delivery '{tag}'", deliveryTag);
                HandleAck(deliveryTag, channelLocallyTransacted);
            }
            catch (Exception e)
            {
                if (_container.CauseChainHasImmediateAcknowledgeRabbitException(e))
                {
                    _logger?.LogDebug("User requested ack for failed delivery: {tag}", deliveryTag);
                    HandleAck(deliveryTag, channelLocallyTransacted);
                }
                else
                {
                    _logger?.LogError(e, "Failed to invoke listener");

                    if (TransactionManager != null)
                    {
                        if (TransactionAttribute.RollbackOn(e))
                        {
                            var resourceHolder = (RabbitResourceHolder)TransactionSynchronizationManager.GetResource(ConnectionFactory);

                            if (resourceHolder == null)
                            {
                                /*
                                 * If we don't actually have a transaction, we have to roll back
                                 * manually. See prepareHolderForRollback().
                                 */
                                Rollback(deliveryTag, e);
                            }

                            throw; // encompassing transaction will handle the rollback.
                        }

                        _logger?.LogDebug(e, "No rollback");
                    }
                    else
                    {
                        Rollback(deliveryTag, e);

                        // no need to rethrow e - we'd ignore it anyway, not throw to client
                    }
                }
            }
        }

        private void HandleAck(ulong deliveryTag, bool channelLocallyTransacted)
        {
            /*
             * If we have a TX Manager, but no TX, act like we are locally transacted.
             */
            bool isLocallyTransacted = channelLocallyTransacted ||
                (_container.IsChannelTransacted && TransactionSynchronizationManager.GetResource(ConnectionFactory) == null);

            try
            {
                if (AckRequired)
                {
                    if (MessagesPerAck > 1)
                    {
                        lock (_lock)
                        {
                            LatestDeferredDeliveryTag = deliveryTag;
                            PendingAcks++;
                            AckIfNecessary(LastAck);
                        }
                    }
                    else if (!_container.IsChannelTransacted || isLocallyTransacted)
                    {
                        Model.BasicAck(deliveryTag, false);
                    }
                }

                if (isLocallyTransacted)
                {
                    RabbitUtils.CommitIfNecessary(Model);
                }
            }
            catch (Exception e)
            {
                AckFailed = true;
                _logger?.LogError(e, "Error acking");
            }
        }

        private void Rollback(ulong deliveryTag, Exception e)
        {
            if (_container.IsChannelTransacted)
            {
                RabbitUtils.RollbackIfNecessary(Model);
            }

            if (AckRequired || ContainerUtils.IsRejectManual(e))
            {
                try
                {
                    if (MessagesPerAck > 1)
                    {
                        lock (_lock)
                        {
                            if (PendingAcks > 0)
                            {
                                SendAck(DateTimeOffset.Now.ToUnixTimeMilliseconds());
                            }
                        }
                    }

                    Model.BasicNack(deliveryTag, true, ContainerUtils.ShouldRequeue(_container.DefaultRequeueRejected, e, _logger));
                }
                catch (Exception e1)
                {
                    _logger?.LogError(e1, "Failed to nack message");
                }
            }

            if (_container.IsChannelTransacted)
            {
                RabbitUtils.CommitIfNecessary(Model);
            }
        }

        private void FinalizeConsumer()
        {
            RabbitUtils.SetPhysicalCloseRequired(Model, true);
            RabbitUtils.CloseChannel(Model);
            RabbitUtils.CloseConnection(_connection);
            _container.CancellationLock.Release(this);
            _container.ConsumerRemoved(this);
        }
    }
}
