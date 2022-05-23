// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Transaction;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Listener.Support;
using Steeltoe.Messaging.RabbitMQ.Support;
using Steeltoe.Messaging.RabbitMQ.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener
{
    public class BlockingQueueConsumer
    {
        private const int DEFAULT_DECLARATION_RETRIES = 3;
        private const int DEFAULT_RETRY_DECLARATION_INTERVAL = 60000;

        public BlockingQueueConsumer(
            IConnectionFactory connectionFactory,
            IMessageHeadersConverter messagePropertiesConverter,
            ActiveObjectCounter<BlockingQueueConsumer> activeObjectCounter,
            AcknowledgeMode acknowledgeMode,
            bool transactional,
            ushort prefetchCount,
            ILoggerFactory loggerFactory,
            params string[] queues)
            : this(connectionFactory, messagePropertiesConverter, activeObjectCounter, acknowledgeMode, transactional, prefetchCount, true, loggerFactory, queues)
        {
        }

        public BlockingQueueConsumer(
            IConnectionFactory connectionFactory,
            IMessageHeadersConverter messagePropertiesConverter,
            ActiveObjectCounter<BlockingQueueConsumer> activeObjectCounter,
            AcknowledgeMode acknowledgeMode,
            bool transactional,
            ushort prefetchCount,
            bool defaultRequeueRejected,
            ILoggerFactory loggerFactory,
            params string[] queues)
            : this(connectionFactory, messagePropertiesConverter, activeObjectCounter, acknowledgeMode, transactional, prefetchCount, defaultRequeueRejected, null, loggerFactory, queues)
        {
        }

        public BlockingQueueConsumer(
            IConnectionFactory connectionFactory,
            IMessageHeadersConverter messagePropertiesConverter,
            ActiveObjectCounter<BlockingQueueConsumer> activeObjectCounter,
            AcknowledgeMode acknowledgeMode,
            bool transactional,
            ushort prefetchCount,
            bool defaultRequeueRejected,
            Dictionary<string, object> consumerArgs,
            ILoggerFactory loggerFactory,
            params string[] queues)
            : this(connectionFactory, messagePropertiesConverter, activeObjectCounter, acknowledgeMode, transactional, prefetchCount, defaultRequeueRejected, consumerArgs, false, loggerFactory, queues)
        {
        }

        public BlockingQueueConsumer(
            IConnectionFactory connectionFactory,
            IMessageHeadersConverter messagePropertiesConverter,
            ActiveObjectCounter<BlockingQueueConsumer> activeObjectCounter,
            AcknowledgeMode acknowledgeMode,
            bool transactional,
            ushort prefetchCount,
            bool defaultRequeueRejected,
            Dictionary<string, object> consumerArgs,
            bool exclusive,
            ILoggerFactory loggerFactory,
            params string[] queues)
            : this(connectionFactory, messagePropertiesConverter, activeObjectCounter, acknowledgeMode, transactional, prefetchCount, defaultRequeueRejected, consumerArgs, false, exclusive, loggerFactory, queues)
        {
        }

        public BlockingQueueConsumer(
            IConnectionFactory connectionFactory,
            IMessageHeadersConverter messagePropertiesConverter,
            ActiveObjectCounter<BlockingQueueConsumer> activeObjectCounter,
            AcknowledgeMode acknowledgeMode,
            bool transactional,
            ushort prefetchCount,
            bool defaultRequeueRejected,
            Dictionary<string, object> consumerArgs,
            bool noLocal,
            bool exclusive,
            ILoggerFactory loggerFactory,
            params string[] queues)
        {
            ConnectionFactory = connectionFactory;
            MessageHeadersConverter = messagePropertiesConverter;
            ActiveObjectCounter = activeObjectCounter;
            AcknowledgeMode = acknowledgeMode;
            Transactional = transactional;
            PrefetchCount = prefetchCount;
            DefaultRequeueRejected = defaultRequeueRejected;
            if (consumerArgs != null && consumerArgs.Count > 0)
            {
                foreach (var arg in consumerArgs)
                {
                    ConsumerArgs.Add(arg.Key, arg.Value);
                }
            }

            NoLocal = noLocal;
            Exclusive = exclusive;
            Queues = queues.ToList();
            Queue = new BlockingCollection<Delivery>(prefetchCount);
            LoggerFactory = loggerFactory;
            Logger = loggerFactory?.CreateLogger<BlockingQueueConsumer>();
        }

        public ILogger<BlockingQueueConsumer> Logger { get; }

        public IMessageHeadersConverter MessageHeadersConverter { get; set; }

        public BlockingCollection<Delivery> Queue { get; }

        public ILoggerFactory LoggerFactory { get; }

        public IConnectionFactory ConnectionFactory { get; }

        public ActiveObjectCounter<BlockingQueueConsumer> ActiveObjectCounter { get; }

        public bool Transactional { get; }

        public ushort PrefetchCount { get; }

        public List<string> Queues { get; }

        public AcknowledgeMode AcknowledgeMode { get; }

        public RC.IModel Channel { get; internal set; }

        public bool Exclusive { get; }

        public bool NoLocal { get; }

        public AtomicBoolean Cancel { get; } = new AtomicBoolean(false);

        public bool DefaultRequeueRejected { get; }

        public Dictionary<string, object> ConsumerArgs { get; } = new Dictionary<string, object>();

        public RC.ShutdownEventArgs Shutdown { get; private set; }

        public HashSet<ulong> DeliveryTags { get; internal set; } = new HashSet<ulong>();

        public long AbortStarted { get; private set; }

        public bool NormalCancel { get; set; }

        public bool Declaring { get; set; }

        public int ShutdownTimeout { get; set; }

        public int DeclarationRetries { get; set; } = DEFAULT_DECLARATION_RETRIES;

        public int FailedDeclarationRetryInterval { get; set; } = AbstractMessageListenerContainer.DEFAULT_FAILED_DECLARATION_RETRY_INTERVAL;

        public int RetryDeclarationInterval { get; set; } = DEFAULT_RETRY_DECLARATION_INTERVAL;

        public IConsumerTagStrategy TagStrategy { get; set; }

        public IBackOffExecution BackOffExecution { get; set; }

        public bool LocallyTransacted { get; set; }

        public int QueueCount => Queues.Count;

        public HashSet<string> MissingQueues => new ();

        public long LastRetryDeclaration { get; set; }

        public RabbitResourceHolder ResourceHolder { get; set; }

        private ConcurrentDictionary<string, InternalConsumer> Consumers { get; } = new ConcurrentDictionary<string, InternalConsumer>();

        public List<string> GetConsumerTags()
        {
            return Consumers
                .Values
                .Select((c) => c.ConsumerTag)
                .Where((tag) => tag != null)
                .ToList();
        }

        public void ClearDeliveryTags()
        {
            DeliveryTags.Clear();
        }

        public IMessage NextMessage()
        {
            Logger?.LogTrace("Retrieving delivery for: {me}", ToString());
            return Handle(Queue.Take());
        }

        public IMessage NextMessage(int timeout)
        {
            Logger?.LogTrace("Retrieving delivery for: {me}", ToString());
            CheckShutdown();
            if (MissingQueues.Count > 0)
            {
                CheckMissingQueues();
            }

            Queue.TryTake(out var item, timeout);
            var message = Handle(item);
            if (message == null && Cancel.Value)
            {
                throw new ConsumerCancelledException();
            }

            return message;
        }

        public void Start()
        {
            Logger?.LogDebug("Starting consumer {consumer}", ToString());
            try
            {
                ResourceHolder = ConnectionFactoryUtils.GetTransactionalResourceHolder(ConnectionFactory, Transactional);
                Channel = ResourceHolder.GetChannel();

                // ClosingRecoveryListener.AddRecoveryListenerIfNecessary(Channel);
            }
            catch (RabbitAuthenticationException e)
            {
                throw new FatalListenerStartupException("Authentication failure", e);
            }

            DeliveryTags.Clear();
            ActiveObjectCounter.Add(this);
            PassiveDeclarations();
            SetQosAndCreateConsumers();
        }

        public void Stop()
        {
            if (AbortStarted == 0)
            {
                AbortStarted = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }

            if (!Cancelled)
            {
                try
                {
                    RabbitUtils.CloseMessageConsumer(Channel, GetConsumerTags(), Transactional);
                }
                catch (Exception e)
                {
                    Logger?.LogDebug(e, "Error closing consumer: {consumer}", ToString());
                }
            }

            Logger?.LogDebug("Closing Rabbit Channel : {channel}", Channel);
            RabbitUtils.SetPhysicalCloseRequired(Channel, true);
            ConnectionFactoryUtils.ReleaseResources(ResourceHolder);
            DeliveryTags.Clear();
            _ = Consumers.TakeWhile((kvp) => Consumers.Count > 0);
            _ = Queue.TakeWhile((d) => Queue.Count > 0);
        }

        public void RollbackOnExceptionIfNecessary(Exception ex)
        {
            var ackRequired = !AcknowledgeMode.IsAutoAck() && (!AcknowledgeMode.IsManual() || ContainerUtils.IsRejectManual(ex));
            try
            {
                if (Transactional)
                {
                    Logger?.LogDebug(ex, "Initiating transaction rollback on application exception");
                    RabbitUtils.RollbackIfNecessary(Channel);
                }

                if (ackRequired)
                {
                    if (DeliveryTags.Count > 0)
                    {
                        var deliveryTag = DeliveryTags.Max();
                        Channel.BasicNack(deliveryTag, true, ContainerUtils.ShouldRequeue(DefaultRequeueRejected, ex, Logger));
                    }

                    if (Transactional)
                    {
                        // Need to commit the reject (=nack)
                        RabbitUtils.CommitIfNecessary(Channel);
                    }
                }
            }
            catch (Exception e)
            {
                Logger?.LogError(ex, "Application exception overridden by rollback exception");
                throw RabbitExceptionTranslator.ConvertRabbitAccessException(e); // NOSONAR stack trace loss
            }
            finally
            {
                DeliveryTags.Clear();
            }
        }

        public bool CommitIfNecessary(bool localTx)
        {
            if (DeliveryTags.Count == 0)
            {
                return false;
            }

            var isLocallyTransacted = localTx || (Transactional && TransactionSynchronizationManager.GetResource(ConnectionFactory) == null);
            try
            {
                var ackRequired = !AcknowledgeMode.IsAutoAck() && !AcknowledgeMode.IsManual();
                if (ackRequired && (!Transactional || isLocallyTransacted))
                {
                    var deliveryTag = new List<ulong>(DeliveryTags)[DeliveryTags.Count - 1];
                    Channel.BasicAck(deliveryTag, true);
                }

                if (isLocallyTransacted)
                {
                    // For manual acks we still need to commit
                    RabbitUtils.CommitIfNecessary(Channel);
                }
            }
            finally
            {
                DeliveryTags.Clear();
            }

            return true;
        }

        public override string ToString()
        {
            return
                $"Consumer@{RuntimeHelpers.GetHashCode(this)}: tags=[{string.Join(',', GetConsumerTags())}], channel={Channel}, acknowledgeMode={AcknowledgeMode} local queue size={Queue.Count}";
        }

        internal List<RC.DefaultBasicConsumer> CurrentConsumers()
        {
            return Consumers.Values.ToList<RC.DefaultBasicConsumer>();
        }

        protected bool HasDelivery => Queue.Count != 0;

        protected bool Cancelled
        {
            get
            {
                return Cancel.Value ||
                    (AbortStarted > 0 && (AbortStarted + ShutdownTimeout) > DateTimeOffset.Now.ToUnixTimeMilliseconds()) ||
                    !ActiveObjectCounter.IsActive;
            }
        }

        protected void BasicCancel()
        {
            BasicCancel(false);
        }

        protected void BasicCancel(bool expected)
        {
            NormalCancel = expected;
            GetConsumerTags().ForEach(consumerTag =>
            {
                if (Channel.IsOpen)
                {
                    RabbitUtils.Cancel(Channel, consumerTag);
                }
            });

            Cancel.Value = true;
            AbortStarted = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        private void PassiveDeclarations()
        {
            // mirrored queue might be being moved
            var passiveDeclareRetries = DeclarationRetries;
            Declaring = true;
            do
            {
                if (Cancelled)
                {
                    break;
                }

                try
                {
                    AttemptPassiveDeclarations();
                    if (passiveDeclareRetries < DeclarationRetries)
                    {
                        Logger?.LogInformation("Queue declaration succeeded after retrying");
                    }

                    passiveDeclareRetries = 0;
                }
                catch (DeclarationException e)
                {
                    HandleDeclarationException(passiveDeclareRetries, e);
                }
            }
            while (passiveDeclareRetries-- > 0 && !Cancelled);
            Declaring = false;
        }

        private void SetQosAndCreateConsumers()
        {
            if (!AcknowledgeMode.IsAutoAck() && !Cancelled)
            {
                // Set basicQos before calling basicConsume (otherwise if we are not acking the broker
                // will send blocks of 100 messages)
                try
                {
                    Channel.BasicQos(0, PrefetchCount, true);
                }
                catch (Exception e)
                {
                    ActiveObjectCounter.Release(this);
                    throw new RabbitIOException(e);
                }
            }

            try
            {
                if (!Cancelled)
                {
                    foreach (var queueName in Queues)
                    {
                        if (!MissingQueues.Contains(queueName))
                        {
                            ConsumeFromQueue(queueName);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw RabbitExceptionTranslator.ConvertRabbitAccessException(e);
            }
        }

        private void HandleDeclarationException(int passiveDeclareRetries, DeclarationException e)
        {
            if (passiveDeclareRetries > 0 && Channel.IsOpen)
            {
                Logger?.LogWarning(e, "Queue declaration failed; retries left={retries}", passiveDeclareRetries);
                try
                {
                    Thread.Sleep(FailedDeclarationRetryInterval);
                }
                catch (Exception e1)
                {
                    Declaring = false;
                    ActiveObjectCounter.Release(this);
                    throw RabbitExceptionTranslator.ConvertRabbitAccessException(e1);
                }
            }
            else if (e.FailedQueues.Count < Queues.Count)
            {
                Logger?.LogWarning("Not all queues are available; only listening on those that are - configured: {queues}; not available: {notavail}", string.Join(',', Queues), string.Join(',', e.FailedQueues));
                lock (MissingQueues)
                {
                    foreach (var q in e.FailedQueues)
                    {
                        MissingQueues.Add(q);
                    }
                }

                LastRetryDeclaration = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
            else
            {
                Declaring = false;
                ActiveObjectCounter.Release(this);
                throw new QueuesNotAvailableException("Cannot prepare queue for listener. Either the queue doesn't exist or the broker will not allow us to use it.", e);
            }
        }

        private void ConsumeFromQueue(string queue)
        {
            var consumer = new InternalConsumer(this, Channel, queue);
            var consumerTag = Channel.BasicConsume(
                                    queue,
                                    AcknowledgeMode.IsAutoAck(),
                                    TagStrategy != null ? TagStrategy.CreateConsumerTag(queue) : string.Empty,
                                    NoLocal,
                                    Exclusive,
                                    ConsumerArgs,
                                    consumer);

            if (consumerTag != null)
            {
                Logger?.LogDebug("Started on queue '{queue}' with tag {consumerTag} : {consumer}", queue, consumerTag, ToString());
            }
            else
            {
                Logger?.LogError("Null consumer tag received for queue: {queue} ", queue);
            }
        }

        private void AttemptPassiveDeclarations()
        {
            DeclarationException failures = null;
            foreach (var queueName in Queues)
            {
                try
                {
                    try
                    {
                        Channel.QueueDeclarePassive(queueName);
                    }
                    catch (RC.Exceptions.WireFormattingException e)
                    {
                        try
                        {
                            if (Channel is IChannelProxy proxy)
                            {
                                proxy.TargetChannel.Close();
                            }
                        }
                        catch (TimeoutException)
                        {
                        }

                        throw new FatalListenerStartupException("Illegal Argument on Queue Declaration", e);
                    }
                }
                catch (RC.Exceptions.RabbitMQClientException e)
                {
                    Logger?.LogWarning("Failed to declare queue: {name} ", queueName);
                    if (!Channel.IsOpen)
                    {
                        throw new RabbitIOException(e);
                    }

                    failures ??= new DeclarationException(e);

                    failures.AddFailedQueue(queueName);
                }
            }

            if (failures != null)
            {
                throw failures;
            }
        }

        private void CheckMissingQueues()
        {
            var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (now - RetryDeclarationInterval > LastRetryDeclaration)
            {
                lock (MissingQueues)
                {
                    var toRemove = new List<string>();
                    Exception error = null;
                    foreach (var queueToCheck in MissingQueues)
                    {
                        var available = true;
                        IConnection connection = null;
                        RC.IModel channelForCheck = null;
                        try
                        {
                            channelForCheck = ConnectionFactory.CreateConnection().CreateChannel();
                            channelForCheck.QueueDeclarePassive(queueToCheck);
                            Logger?.LogInformation("Queue '{queue}' is now available", queueToCheck);
                        }
                        catch (Exception e)
                        {
                            available = false;
                            Logger?.LogWarning(e, "Queue '{queue}' is not available", queueToCheck);
                        }
                        finally
                        {
                            RabbitUtils.CloseChannel(channelForCheck);
                            RabbitUtils.CloseConnection(connection);
                        }

                        if (available)
                        {
                            try
                            {
                                ConsumeFromQueue(queueToCheck);
                                toRemove.Add(queueToCheck);
                            }
                            catch (Exception e)
                            {
                                error = e;
                                break;
                            }
                        }
                    }

                    if (toRemove.Count > 0)
                    {
                        foreach (var remove in toRemove)
                        {
                            MissingQueues.Remove(remove);
                        }
                    }

                    if (error != null)
                    {
                        throw RabbitExceptionTranslator.ConvertRabbitAccessException(error);
                    }
                }

                LastRetryDeclaration = now;
            }
        }

        private void CheckShutdown()
        {
            if (Shutdown != null)
            {
                throw new ShutdownSignalException(Shutdown);
            }
        }

        private IMessage Handle(Delivery delivery)
        {
            if (delivery == null && Shutdown != null)
            {
                throw new ShutdownSignalException(Shutdown);
            }

            if (delivery == null)
            {
                return null;
            }

            var body = delivery.Body;
            var messageProperties = MessageHeadersConverter.ToMessageHeaders(delivery.Properties, delivery.Envelope, EncodingUtils.Utf8);
            var accesor = RabbitHeaderAccessor.GetMutableAccessor(messageProperties);
            accesor.ConsumerTag = delivery.ConsumerTag;
            accesor.ConsumerQueue = delivery.Queue;
            var message = Message.Create(body, accesor.MessageHeaders);
            Logger?.LogDebug("Received message: {message}", message);
            if (messageProperties.DeliveryTag() != null)
            {
                DeliveryTags.Add(messageProperties.DeliveryTag().Value);
            }

            if (Transactional && !LocallyTransacted)
            {
                ConnectionFactoryUtils.RegisterDeliveryTag(ConnectionFactory, Channel, delivery.Envelope.DeliveryTag);
            }

            return message;
        }

        private class InternalConsumer : RC.DefaultBasicConsumer
        {
            public InternalConsumer(BlockingQueueConsumer consumer, RC.IModel channel, string queue, ILogger<InternalConsumer> logger = null)
                : base(channel)
            {
                Consumer = consumer;
                QueueName = queue;
                Logger = logger;
            }

            public BlockingQueueConsumer Consumer { get; }

            public string QueueName { get; }

            public ILogger<InternalConsumer> Logger { get; }

            public bool Canceled { get; set; }

            public override void HandleBasicConsumeOk(string consumerTag)
            {
                base.HandleBasicConsumeOk(consumerTag);
                ConsumerTag = consumerTag;
                Logger?.LogDebug("ConsumeOK: {consumer} {consumerTag}", Consumer.ToString(), consumerTag);
                Consumer.Consumers.TryAdd(QueueName, this);

                // if (BlockingQueueConsumer.this.applicationEventPublisher != null) {
                //    BlockingQueueConsumer.this.applicationEventPublisher
                //            .publishEvent(new ConsumeOkEvent(this, this.queueName, consumerTag));
                // }
            }

            public override void HandleModelShutdown(object model, RC.ShutdownEventArgs reason)
            {
                base.HandleModelShutdown(model, reason);

                Logger?.LogDebug("Received shutdown signal for consumer tag: {tag} reason: {reason}", ConsumerTag, reason.ReplyText);
                Consumer.Shutdown = reason;
                Consumer.DeliveryTags.Clear();
                Consumer.ActiveObjectCounter.Release(Consumer);
            }

            public override void HandleBasicCancel(string consumerTag)
            {
                Logger?.LogWarning("Cancel received for {consumerTag} : {queueName} : {consumer}", consumerTag, QueueName, ToString());
                Consumer.Consumers.Remove(QueueName, out var me);
                if (Consumer.Consumers.Count != 0)
                {
                    Consumer.BasicCancel(false);
                }
                else
                {
                    Consumer.Cancel.Value = true;
                }
            }

            public override void HandleBasicCancelOk(string consumerTag)
            {
                Logger?.LogDebug("Received CancelOk for {consumerTag} : {queueName} : {consumer}", consumerTag, QueueName, ToString());
                Canceled = true;
            }

            public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, RC.IBasicProperties properties, byte[] body)
            {
                Logger?.LogDebug("Storing delivery for consumer tag: {tag} with deliveryTag: {deliveryTag} for consumer: {consumer}", ConsumerTag, deliveryTag, ToString());
                try
                {
                    var delivery = new Delivery(consumerTag, new Envelope(deliveryTag, redelivered, exchange, routingKey), properties, body, QueueName);
                    if (Consumer.AbortStarted > 0)
                    {
                        if (!Consumer.Queue.TryAdd(delivery, Consumer.ShutdownTimeout))
                        {
                            RabbitUtils.SetPhysicalCloseRequired(Model, true);
                            _ = Consumer.Queue.TakeWhile((d) => Consumer.Queue.Count > 0);
                            if (!Canceled)
                            {
                                RabbitUtils.Cancel(Model, consumerTag);
                            }

                            try
                            {
                                Model.Close();
                            }
                            catch (Exception)
                            {
                                // Noop
                            }
                        }
                    }
                    else
                    {
                        Consumer.Queue.TryAdd(delivery);
                    }
                }
                catch (Exception e)
                {
                    Logger?.LogWarning(e, "Unexpected exception during delivery");
                }
            }

            public override string ToString()
            {
                return $"InternalConsumer{{queue='{QueueName}', consumerTag='{ConsumerTag}'}}";
            }
        }

        private class DeclarationException : RabbitException
        {
            public DeclarationException()
                : base("Failed to declare queue(s):")
            {
            }

            public DeclarationException(Exception e)
                : base("Failed to declare queue(s):", e)
            {
            }

            public List<string> FailedQueues { get; } = new List<string>();

            public void AddFailedQueue(string queue)
            {
                FailedQueues.Add(queue);
            }

            public override string Message => base.Message + string.Join(',', FailedQueues);
        }
    }
}