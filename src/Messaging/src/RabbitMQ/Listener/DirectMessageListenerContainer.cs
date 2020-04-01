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
using RabbitMQ.Client.Exceptions;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Transaction;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Data;
using Steeltoe.Messaging.Rabbit.Exceptions;
using Steeltoe.Messaging.Rabbit.Listener.Support;
using Steeltoe.Messaging.Rabbit.Transaction;
using Steeltoe.Messaging.Rabbit.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using R = RabbitMQ.Client;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public class DirectMessageListenerContainer : AbstractMessageListenerContainer
    {
        protected const int START_WAIT_TIME = 60;

        protected const int DEFAULT_MONITOR_INTERVAL = 10_000;

        protected const int DEFAULT_ACK_TIMEOUT = 20_000;

        protected readonly List<SimpleConsumer> _consumers = new List<SimpleConsumer>();

        protected readonly ActiveObjectCounter<SimpleConsumer> _cancellationLock = new ActiveObjectCounter<SimpleConsumer>();

        protected readonly List<SimpleConsumer> _consumersToRestart = new List<SimpleConsumer>();

        protected readonly Dictionary<string, List<SimpleConsumer>> _consumersByQueue = new Dictionary<string, List<SimpleConsumer>>();

        private int _consumersPerQueue = 1;

        private volatile bool _started = false;

        private volatile bool _aborted = false;

        private volatile bool _hasStopped = false;

        private long _lastAlertAt;

        private CountdownEvent _startedLatch = new CountdownEvent(1);

        private Task _consumerMonitorTask;

        private CancellationTokenSource _consumerMonitorCancelationToken;

        public DirectMessageListenerContainer(string name = null, ILogger logger = null)
            : this(null, null, name, logger)
        {
        }

        public DirectMessageListenerContainer(IApplicationContext applicationContext, string name = null, ILogger logger = null)
            : this(applicationContext, null, name, logger)
        {
        }

        public DirectMessageListenerContainer(IApplicationContext applicationContext, IConnectionFactory connectionFactory, string name = null, ILogger logger = null)
            : base(applicationContext, connectionFactory, name, logger)
        {
            MissingQueuesFatal = false;
        }

        public virtual int ConsumersPerQueue
        {
            get
            {
                return _consumersPerQueue;
            }

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
            get
            {
                return base.Exclusive;
            }

            set
            {
                if (value && ConsumersPerQueue != 1)
                {
                    throw new ArgumentException("When the consumer is exclusive, the consumers per queue must be 1");
                }

                base.Exclusive = value;
            }
        }

        public virtual long MonitorInterval { get; set; } = DEFAULT_MONITOR_INTERVAL;

        public virtual int MessagesPerAck { get; set; }

        public virtual long AckTimeout { get; set; } = DEFAULT_ACK_TIMEOUT;

        public virtual long LastRestartAttempt { get; private set; }

        public override void SetQueueNames(params string[] queueNames)
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("Cannot set queue names while running, use add/remove");
            }

            base.SetQueueNames(queueNames);
        }

        public override void AddQueueNames(params string[] queueNames)
        {
            if (queueNames == null)
            {
                throw new ArgumentNullException(nameof(queueNames));
            }

            try
            {
                var names = queueNames.Select((n) => n ?? throw new ArgumentNullException("queue names cannot be null"));
                AddQueues(names);
            }
            catch (Exception e)
            {
                _logger?.LogError("Failed to add queue names", e);
                throw;
            }

            base.AddQueueNames(queueNames);
        }

        public override void AddQueues(params Config.Queue[] queues)
        {
            if (queues == null)
            {
                throw new ArgumentNullException(nameof(queues));
            }

            try
            {
                var names = queues.Select((q) => q != null ? q.Name : throw new ArgumentNullException("queues cannot contain nulls"));
                AddQueues(names);
            }
            catch (Exception e)
            {
                _logger?.LogError("Failed to add queues", e);
                throw;
            }

            base.AddQueues(queues);
        }

        public override bool RemoveQueueNames(params string[] queueNames)
        {
            RemoveQueues(queueNames.AsEnumerable());
            return base.RemoveQueueNames(queueNames);
        }

        public override void RemoveQueues(params Config.Queue[] queues)
        {
            RemoveQueues(queues.Select((q) => q.ActualName));
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
            var queueNames = GetQueueNames();
            CheckMissingQueues(queueNames);

            if (IdleEventInterval > 0 && MonitorInterval > IdleEventInterval)
            {
                MonitorInterval = IdleEventInterval / 2;
            }

            if (FailedDeclarationRetryInterval < MonitorInterval)
            {
                MonitorInterval = FailedDeclarationRetryInterval;
            }

            var namesToQueues = GetQueueNamesToQueues();
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
                _startedLatch.Signal();
            }

            _logger?.LogInformation("Container initialized for queues: " + string.Join(',', queueNames));
        }

        protected virtual void DoRedeclareElementsIfNecessary()
        {
            var routingLookupKey = GetRoutingLookupKey();
            if (routingLookupKey != null)
            {
                SimpleResourceHolder.Push(GetRoutingConnectionFactory(), routingLookupKey);
            }

            try
            {
                RedeclareElementsIfNecessary();
            }
            catch (Exception e)
            {
                _logger?.LogError("Failed to redeclare elements", e);
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
            var waitForConsumers = false;

            lock (_consumersMonitor)
            {
                if (_started || _aborted)
                {
                    // Copy in the same order to avoid ConcurrentModificationException during remove in the
                    // cancelConsumer().
                    canceledConsumers = new List<SimpleConsumer>(_consumers);
                    ActualShutDown(canceledConsumers);
                    waitForConsumers = true;
                }
            }

            if (waitForConsumers)
            {
                try
                {
                    if (_cancellationLock.Wait(TimeSpan.FromMilliseconds(ShutdownTimeout)))
                    {
                        _logger?.LogInformation("Successfully waited for consumers to finish.");
                    }
                    else
                    {
                        _logger?.LogInformation("Consumers not finished.");
                        if (ForceCloseChannel)
                        {
                            canceledConsumers.ForEach(consumer =>
                            {
                                var eventMessage = "Closing channel for unresponsive consumer: " + consumer;
                                _logger?.LogWarning(eventMessage);
                                consumer.CancelConsumer(eventMessage);
                            });
                        }
                    }
                }
                catch (Exception e)
                {
                    // Thread.currentThread().interrupt();
                    _logger?.LogWarning("Interrupted waiting for consumers. Continuing with shutdown.", e);
                }
                finally
                {
                    _startedLatch = new CountdownEvent(1);
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

        private void CheckMissingQueues(string[] queueNames)
        {
            if (MissingQueuesFatal)
            {
                var checkAdmin = AmqpAdmin;
                if (checkAdmin == null)
                {
                    checkAdmin = new RabbitAdmin(ConnectionFactory);
                    AmqpAdmin = checkAdmin;
                }

                foreach (var queue in queueNames)
                {
                    var queueProperties = checkAdmin.GetQueueProperties(queue);
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
            _logger?.LogDebug("Shutting down");
            consumers.ForEach((c) => CancelConsumer(c));
            _consumers.Clear();
            _consumersByQueue.Clear();
            _logger?.LogDebug("All consumers canceled");
            if (_consumerMonitorTask != null)
            {
                _consumerMonitorCancelationToken.Cancel();
                _consumerMonitorTask = null;
            }
        }

        private void ConsumeFromQueue(string queue)
        {
            _consumersByQueue.TryGetValue(queue, out var list);

            // Possible race with setConsumersPerQueue and the task launched by start()
            if (list == null || list.Count == 0)
            {
                for (var i = 0; i < _consumersPerQueue; i++)
                {
                    DoConsumeFromQueue(queue);
                }
            }
        }

        private void DoConsumeFromQueue(string queue)
        {
            if (!IsActive)
            {
                _logger?.LogDebug("Consume from queue " + queue + " ignore, container stopping");
                return;
            }

            var routingLookupKey = GetRoutingLookupKey();
            if (routingLookupKey != null)
            {
                SimpleResourceHolder.Push(GetRoutingConnectionFactory(), routingLookupKey);
            }

            Connection.IConnection connection = null;
            try
            {
                connection = ConnectionFactory.CreateConnection();
            }
            catch (Exception e)
            {
                // publishConsumerFailedEvent(e.getMessage(), false, e);
                _logger?.LogError("Exception while CreateConnection", e);
                AddConsumerToRestart(new SimpleConsumer(this, null, null, queue));
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

            var consumer = Consume(queue, connection);
            lock (_consumersMonitor)
            {
                if (consumer != null)
                {
                    _cancellationLock.Add(consumer);
                    _consumers.Add(consumer);
                    _consumersByQueue.TryGetValue(queue, out var list);
                    if (list == null)
                    {
                        list = new List<SimpleConsumer>();
                        _consumersByQueue.Add(queue, list);
                    }

                    list.Add(consumer);
                    _logger?.LogInformation(consumer + " started");

                    // if (getApplicationEventPublisher() != null)
                    // {
                    //    getApplicationEventPublisher().publishEvent(new AsyncConsumerStartedEvent(this, consumer));
                    // }
                }
            }
        }

        private SimpleConsumer Consume(string queue, Connection.IConnection connection)
        {
            R.IModel channel = null;
            SimpleConsumer consumer = null;
            try
            {
                channel = connection.CreateChannel(IsChannelTransacted);
                channel.BasicQos(0, (ushort)PrefetchCount, false);  // TODO: Verify this
                consumer = new SimpleConsumer(this, connection, channel, queue);
                channel.QueueDeclarePassive(queue);
                consumer.ConsumerTag = channel.BasicConsume(
                    queue,
                    AcknowledgeMode.IsAutoAck(),
                    ConsumerTagStrategy != null ? ConsumerTagStrategy.CreateConsumerTag(queue) : string.Empty,
                    NoLocal,
                    Exclusive,
                    ConsumerArguments,
                    consumer);
            }

            // catch (AmqpApplicationContextClosedException e)
            // {
            //    throw new AmqpConnectException(e);
            // }
            catch (Exception e)
            {
                RabbitUtils.CloseChannel(channel, _logger);
                RabbitUtils.CloseConnection(connection, _logger);

                consumer = HandleConsumeException(queue, consumer, e);
            }

            return consumer;
        }

        private SimpleConsumer HandleConsumeException(string queue, SimpleConsumer consumerArg, Exception e)
        {
            var consumer = consumerArg;

            // if (e.getCause() is ShutdownSignalException  && e.getCause().getMessage().contains("in exclusive use")) {
            //        getExclusiveConsumerExceptionLogger().log(logger,
            //                "Exclusive consumer failure", e.getCause());
            //        publishConsumerFailedEvent("Consumer raised exception, attempting restart", false, e);
            //    }

            // else if (e.getCause() is ShutdownSignalException  && RabbitUtils.isPassiveDeclarationChannelClose((ShutdownSignalException)e.getCause())) {
            //        this.logger.error("Queue not present, scheduling consumer "
            //                + (consumer == null ? "for queue " + queue : consumer) + " for restart", e);
            //    }
            // else if (this.logger.isWarnEnabled())
            //    {
            _logger?.LogWarning("basicConsume failed, scheduling consumer " + consumer == null ? "for queue " + queue.ToString() : consumer.ToString() + " for restart", e);

            // }
            if (consumer == null)
            {
                AddConsumerToRestart(new SimpleConsumer(this, null, null, queue));
            }
            else
            {
                AddConsumerToRestart(consumer);
                consumer = null;
            }

            return consumer;
        }

        private void StartMonitor(long idleEventInterval, Dictionary<string, Config.Queue> namesToQueues)
        {
            _consumerMonitorCancelationToken = new CancellationTokenSource();
            _consumerMonitorTask = Task.Run(
                async () =>
                {
                    while (!_consumerMonitorCancelationToken.Token.IsCancellationRequested)
                    {
                        var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        CheckIdle(idleEventInterval, now);
                        CheckConsumers(now);

                        if (LastRestartAttempt + FailedDeclarationRetryInterval < now)
                        {
                            lock (_consumersMonitor)
                            {
                                var restartableConsumers = new List<SimpleConsumer>(_consumersToRestart);
                                _consumersToRestart.Clear();
                                if (_started)
                                {
                                    if (restartableConsumers.Count > 0)
                                    {
                                        DoRedeclareElementsIfNecessary();
                                    }

                                    foreach (var consumer in restartableConsumers)
                                    {
                                        if (!_consumersByQueue.ContainsKey(consumer.Queue))
                                        {
                                            _logger?.LogDebug("Skipping restart of consumer " + consumer);
                                            continue;
                                        }

                                        _logger?.LogDebug("Attempting to restart consumer " + consumer);
                                        if (!RestartConsumer(namesToQueues, restartableConsumers, consumer))
                                        {
                                            break;
                                        }
                                    }

                                    LastRestartAttempt = now;
                                }
                            }
                        }

                        ProcessMonitorTask();

                        await Task.Delay(TimeSpan.FromMilliseconds(MonitorInterval), _consumerMonitorCancelationToken.Token);
                    }
                }, _consumerMonitorCancelationToken.Token);
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
                _logger?.LogDebug("Canceling " + consumer);
                lock (consumer)
                {
                    consumer.Canceled = true;
                    if (MessagesPerAck > 1)
                    {
                        try
                        {
                            consumer.AckIfNecessary(0L);
                        }
                        catch (IOException e)
                        {
                            _logger?.LogError("Exception while sending delayed ack", e);
                        }
                    }
                }

                RabbitUtils.Cancel(consumer.Model, consumer.ConsumerTag, _logger);
            }
            finally
            {
                _consumers.Remove(consumer);
                ConsumerRemoved(consumer);
            }
        }

        private void CheckConsumers(long now)
        {
            List<SimpleConsumer> consumersToCancel;
            lock (_consumersMonitor)
            {
                consumersToCancel = _consumers
                    .Where(consumer =>
                    {
                        var open = consumer.Model.IsOpen && !consumer.AckFailed && !consumer.TargetChanged;
                        if (open && MessagesPerAck > 1)
                        {
                            try
                            {
                                consumer.AckIfNecessary(now);
                            }
                            catch (IOException e)
                            {
                                _logger?.LogError("Exception while sending delayed ack", e);
                            }
                        }

                        return !open;
                    })
                    .ToList();
            }

            consumersToCancel
                .ForEach(consumer =>
                {
                    try
                    {
                        RabbitUtils.CloseMessageConsumer(consumer.Model, new List<string>() { consumer.ConsumerTag }, IsChannelTransacted, _logger);
                    }
                    catch (Exception e)
                    {
                        _logger?.LogDebug("Error closing consumer " + consumer, e);
                    }

                    _logger?.LogError("Consumer canceled - channel closed " + consumer);
                    consumer.CancelConsumer("Consumer " + consumer + " channel closed");
                });
        }

        private bool RestartConsumer(Dictionary<string, Config.Queue> namesToQueues, List<SimpleConsumer> restartableConsumers, SimpleConsumer consumerArg)
        {
            var consumer = consumerArg;
            namesToQueues.TryGetValue(consumer.Queue, out var queue);
            if (queue != null && string.IsNullOrEmpty(queue.Name))
            {
                // check to see if a broker-declared queue name has changed
                var actualName = queue.ActualName;
                if (!string.IsNullOrEmpty(actualName))
                {
                    namesToQueues.Remove(consumer.Queue);
                    namesToQueues[actualName] = queue;
                    consumer = new SimpleConsumer(this, null, null, actualName);
                }
            }

            try
            {
                DoConsumeFromQueue(consumer.Queue);
                return true;
            }
            catch (Exception e)
            {
                _logger?.LogError("Cannot connect to server", e);

                // if (e.getCause() instanceof AmqpApplicationContextClosedException) {
                //    this.logger.error("Application context is closed, terminating");
                //    this.taskScheduler.schedule(this::stop, new Date());
                // }
                _consumersToRestart.AddRange(restartableConsumers);
                _logger?.LogTrace("After restart exception, consumers to restart now: " + _consumersToRestart);
                return false;
            }
        }

        private void StartConsumers(string[] queueNames)
        {
            lock (_consumersMonitor)
            {
                if (_hasStopped)
                {
                    // container stopped before we got the lock
                    _logger?.LogDebug("Consumer start aborted - container stopping");
                }
                else
                {
                    var backOffExecution = RecoveryBackOff.Start();
                    while (!_started && IsRunning)
                    {
                        _cancellationLock.Reset();
                        try
                        {
                            foreach (var queue in queueNames)
                            {
                                ConsumeFromQueue(queue);
                            }
                        }
                        catch (Exception e) when (e is ConnectFailureException || e is IOException)
                        {
                            var nextBackOff = backOffExecution.NextBackOff();
                            if (nextBackOff < 0)
                            {
                                _aborted = true;
                                Shutdown();
                                _logger?.LogError("Failed to start container - fatal error or backOffs exhausted", e);
                                Task.Run(() => Stop());

                                // this.taskScheduler.schedule(this::stop, new Date());
                                break;
                            }

                            _logger?.LogError("Error creating consumer; retrying in " + nextBackOff, e);
                            DoShutdown();
                            try
                            {
                                Thread.Sleep(nextBackOff); // NOSONAR
                            }
                            catch (Exception e1)
                            {
                                _logger?.LogError("Exception while in backoff;" + nextBackOff, e1);

                                // Thread.currentThread().interrupt();
                            }

                            // initialization failed; try again having rested for backOff-interval
                            continue;
                        }

                        _started = true;
                        _startedLatch.Signal();
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
                    if (!_startedLatch.Wait(TimeSpan.FromSeconds(START_WAIT_TIME)))
                    {
                        throw new InvalidOperationException("Container is not started - cannot adjust queues");
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogError("Exception thrown while waiting for container start", e);
                    throw;
                }
            }
        }

        private void AddQueues(IEnumerable<string> names)
        {
            if (IsRunning)
            {
                lock (_consumersMonitor)
                {
                    CheckStartState();
                    var current = GetQueueNamesAsSet();
                    foreach (var name in names)
                    {
                        if (current.Contains(name))
                        {
                            _logger?.LogWarning("Queue " + name + " is already configured for this container: " + this + ", ignoring add");
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
                lock (_consumersMonitor)
                {
                    CheckStartState();
                    foreach (var name in queueNames)
                    {
                        if (name != null && _consumersByQueue.TryGetValue(name, out var consumers))
                        {
                            foreach (var consumer in consumers)
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
            lock (_consumersMonitor)
            {
                CheckStartState();
                _consumersToRestart.Clear();
                foreach (var queue in GetQueueNames())
                {
                    _consumersByQueue.TryGetValue(queue, out var consumers);
                    while (consumers != null && consumers.Count < newCount)
                    {
                        DoConsumeFromQueue(queue);
                    }

                    if (consumers != null && consumers.Count > newCount)
                    {
                        var delta = consumers.Count - newCount;
                        for (var i = 0; i < delta; i++)
                        {
                            var index = FindIdleConsumer();
                            if (index >= 0)
                            {
                                var consumer = consumers[index];
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
                _consumersToRestart.Add(consumer);
                _logger?.LogTrace("Consumers to restart now: " + _consumersToRestart.Count);
            }
        }

        protected class SimpleConsumer : R.DefaultBasicConsumer
        {
            private readonly DirectMessageListenerContainer _container;
            private readonly Connection.IConnection _connection;
            private readonly R.IModel _targetChannel;
            private readonly ILogger _logger;
            private readonly object _lock = new object();

            public SimpleConsumer(DirectMessageListenerContainer container, Connection.IConnection connection, R.IModel channel, string queue, ILogger logger = null)
                : base(channel)
            {
                _container = container;
                _connection = connection;
                Queue = queue;
                AckRequired = !_container.AcknowledgeMode.IsAutoAck() && !_container.AcknowledgeMode.IsManual();
                if (channel is IChannelProxy)
                {
                    _targetChannel = ((IChannelProxy)channel).TargetChannel;
                }
                else
                {
                    _targetChannel = null;
                }

                _logger = logger;
                TransactionManager = _container.TransactionManager;
                TransactionAttribute = _container.TransactionAttribute;
                IsRabbitTxManager = TransactionManager is RabbitTransactionManager;
                ConnectionFactory = _container.ConnectionFactory;
                MessagesPerAck = _container.MessagesPerAck;
                LastAck = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                AckTimeout = _container.AckTimeout;
            }

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

            public bool TargetChanged
            {
                get
                {
                    return _targetChannel != null && !((IChannelProxy)Model).TargetChannel.Equals(_targetChannel);
                }
            }

            public int IncrementAndGetEpoch()
            {
                Epoch = Epoch + 1;
                return Epoch;
            }

            public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, R.IBasicProperties properties, byte[] body)
            {
                var envelope = new Envelope(deliveryTag, redelivered, exchange, routingKey);
                var messageProperties = _container.MessagePropertiesConverter.ToMessageProperties(properties, envelope, EncodingUtils.Utf8);
                messageProperties.ConsumerTag = consumerTag;
                messageProperties.ConsumerQueue = Queue;
                var message = new Message(body, messageProperties);
                _logger?.LogDebug(this + " received " + message);
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
                    catch (Exception e)
                    {
                        // NOSONAR
                    }
                }
            }

            public override void HandleBasicConsumeOk(string consumerTag)
            {
                base.HandleBasicConsumeOk(consumerTag);
                _logger?.LogDebug("New " + this + " consumeOk");
            }

            public override void HandleBasicCancelOk(string consumerTag)
            {
                _logger?.LogDebug("CancelOk " + this);
                FinalizeConsumer();
            }

            public override void HandleBasicCancel(string consumerTag)
            {
                _logger?.LogError("Consumer canceled - queue deleted? " + this);
                CancelConsumer("Consumer " + this + " canceled");
            }

            public override string ToString()
            {
                return "SimpleConsumer [queue=" + Queue + ", consumerTag=" + ConsumerTag + " identity=" + GetHashCode() + "]";
            }

            internal void CancelConsumer(string eventMessage)
            {
                lock (_container._consumersMonitor)
                {
                    if (_container._consumersByQueue.TryGetValue(Queue, out var list))
                    {
                        list.Remove(this);
                    }

                    _container._consumers.Remove(this);
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

            private void ExecuteListenerInTransaction(Message message, ulong deliveryTag)
            {
                if (IsRabbitTxManager)
                {
                    ConsumerChannelRegistry.RegisterConsumerChannel(Model, ConnectionFactory);
                }

                if (TransactionTemplate == null)
                {
                    TransactionTemplate = new TransactionTemplate(TransactionManager, TransactionAttribute);
                }

                TransactionTemplate.Execute<object>(s =>
                {
                    var resourceHolder = ConnectionFactoryUtils.BindResourceToTransaction(new RabbitResourceHolder(Model, false), ConnectionFactory, true);
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
                    //    //NOSONAR ok to catch Throwable here because we re-throw it below
                    //    throw new WrappedTransactionException(e2);
                    // }
                    return null;
                });
            }

            private void CallExecuteListener(Message message, ulong deliveryTag)
            {
                var channelLocallyTransacted = _container.IsChannelLocallyTransacted;
                try
                {
                    _container.ExecuteListener(Model, message);
                    HandleAck(deliveryTag, channelLocallyTransacted);
                }
                catch (ImmediateAcknowledgeAmqpException e)
                {
                    _logger?.LogDebug("User requested ack for failed delivery '" + e.Message + "': " + deliveryTag);
                    HandleAck(deliveryTag, channelLocallyTransacted);
                }
                catch (Exception e)
                {
                    if (_container.CauseChainHasImmediateAcknowledgeAmqpException(e))
                    {
                        _logger?.LogDebug("User requested ack for failed delivery: " + deliveryTag);
                        HandleAck(deliveryTag, channelLocallyTransacted);
                    }
                    else
                    {
                        _logger?.LogError("Failed to invoke listener", e);
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
                            else
                            {
                                _logger?.LogDebug("No rollback for " + e);
                            }
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
                var isLocallyTransacted = channelLocallyTransacted ||
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
                    _logger?.LogError("Error acking", e);
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
                    catch (IOException e1)
                    {
                        _logger?.LogError("Failed to nack message", e1);
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
                _container._cancellationLock.Release(this);
                _container.ConsumerRemoved(this);
            }
        }
    }
}
