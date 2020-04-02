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
using RabbitMQ.Client;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Retry;
using Steeltoe.Common.Services;
using Steeltoe.Messaging.Rabbit.Config;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Steeltoe.Messaging.Rabbit.Connection.CachingConnectionFactory;

namespace Steeltoe.Messaging.Rabbit.Core
{
    public class RabbitAdmin : IAmqpAdmin, IConnectionListener, IServiceNameAware
    {
        public const string DEFAULT_RABBIT_ADMIN_SERVICE_NAME = "rabbitAdmin";

        public const string QUEUE_NAME = "QUEUE_NAME";
        public const string QUEUE_MESSAGE_COUNT = "QUEUE_MESSAGE_COUNT";
        public const string QUEUE_CONSUMER_COUNT = "QUEUE_CONSUMER_COUNT";

        private const int DECLARE_MAX_ATTEMPTS = 5;
        private const int DECLARE_INITIAL_RETRY_INTERVAL = 1000;
        private const int DECLARE_MAX_RETRY_INTERVAL = 5000;
        private const double DECLARE_RETRY_MULTIPLIER = 2.0d;
        private const string DELAYED_MESSAGE_EXCHANGE = "x-delayed-message";

        private readonly object _lifecycleMonitor = new object();
        private ILogger _logger;
        private int _initializing = 0;

        public RabbitAdmin(IApplicationContext applicationContext, Connection.IConnectionFactory connectionFactory, ILogger logger = null)
        {
            if (connectionFactory == null)
            {
                throw new ArgumentNullException(nameof(connectionFactory));
            }

            _logger = logger;
            ApplicationContext = applicationContext;
            ConnectionFactory = connectionFactory;
            RabbitTemplate = new RabbitTemplate(connectionFactory);
            DoInitialize();
        }

        public RabbitAdmin(Connection.IConnectionFactory connectionFactory, ILogger logger = null)
            : this(null, connectionFactory, logger)
        {
        }

        public IApplicationContext ApplicationContext { get; set; }

        public string Name { get; set; } = DEFAULT_RABBIT_ADMIN_SERVICE_NAME;

        public Connection.IConnectionFactory ConnectionFactory { get; }

        public RabbitTemplate RabbitTemplate { get; }

        public bool AutoStartup { get; set; }

        public bool IgnoreDeclarationExceptions { get; set; }

        public DeclarationExceptionEvent LastDeclarationExceptionEvent { get; private set; }

        public bool ExplicitDeclarationsOnly { get; set; }

        public bool IsAutoStartup { get; set; } = true;

        public bool IsRunning { get; private set; }

        public RetryTemplate RetryTemplate { get; set; }

        public bool RetryDisabled { get; set; } = false;

        public void DeclareBinding(Binding binding)
        {
            try
            {
                RabbitTemplate.Execute<object>(
                    channel =>
                    {
                        DeclareBindings(channel, binding);
                        return null;
                    });
            }
            catch (AmqpException e)
            {
                LogOrRethrowDeclarationException(binding, "binding", e);
            }
        }

        public void DeclareExchange(IExchange exchange)
        {
            try
            {
                RabbitTemplate.Execute<object>(channel =>
               {
                   DeclareExchanges(channel, exchange);
                   return null;
               });
            }
            catch (AmqpException e)
            {
                LogOrRethrowDeclarationException(exchange, "exchange", e);
            }
        }

        public Queue DeclareQueue()
        {
            try
            {
                var declareOk = RabbitTemplate.Execute(
                    channel =>
                    {
                        return channel.QueueDeclare();
                    });
                return new Queue(declareOk.QueueName, false, true, true); // NOSONAR never null
            }
            catch (AmqpException e)
            {
                LogOrRethrowDeclarationException(null, "queue", e);
                return null;
            }
        }

        public string DeclareQueue(Queue queue)
        {
            try
            {
                return RabbitTemplate.Execute(
                    channel =>
                    {
                        var declared = DeclareQueues(channel, queue);
                        return declared.Count > 0 ? declared[0].QueueName : null;
                    });
            }
            catch (AmqpException e)
            {
                LogOrRethrowDeclarationException(queue, "queue", e);
                return null;
            }
        }

        public bool DeleteExchange(string exchangeName)
        {
            return RabbitTemplate.Execute(
                channel =>
                {
                    if (IsDeletingDefaultExchange(exchangeName))
                    {
                        return true;
                    }

                    try
                    {
                        channel.ExchangeDelete(exchangeName, true);
                    }
                    catch (IOException e)
                    {
                        _logger?.LogError("Exception while issuing ExchangeDelete", e);
                        return false;
                    }

                    return true;
                });
        }

        public bool DeleteQueue(string queueName)
        {
            return RabbitTemplate.Execute(
                channel =>
                {
                    try
                    {
                        channel.QueueDelete(queueName);
                        return true;
                    }
                    catch (IOException e)
                    {
                        _logger?.LogError("Exception while issuing QueueDelete", e);
                        return false;
                    }
                });
        }

        public void DeleteQueue(string queueName, bool unused, bool empty)
        {
            RabbitTemplate.Execute<object>(
                channel =>
                {
                    channel.QueueDelete(queueName, unused, empty);
                    return null;
                });
        }

        public QueueInformation GetQueueInfo(string queueName)
        {
            if (string.IsNullOrEmpty(queueName))
            {
                throw new ArgumentNullException(nameof(queueName));
            }

            if (queueName.Length > 255)
            {
                return null;
            }

            return RabbitTemplate.Execute(
                channel =>
                {
                    try
                    {
                        var declareOk = channel.QueueDeclarePassive(queueName);
                        return new QueueInformation(declareOk.QueueName, declareOk.MessageCount, declareOk.ConsumerCount);
                    }
                    catch (IOException e)
                    {
                        _logger?.LogError("Exception while fetching Queue properties: '" + queueName + "'", e);
                        try
                        {
                            if (channel is IChannelProxy)
                            {
                                ((IChannelProxy)channel).TargetChannel.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError("Exception while closing channel '" + queueName + "'", ex);
                        }

                        return null;
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError("Queue '" + queueName + "' does not exist", e);
                        return null;
                    }
                });
        }

        public Dictionary<string, object> GetQueueProperties(string queueName)
        {
            var queueInfo = GetQueueInfo(queueName);
            if (queueInfo != null)
            {
                var props = new Dictionary<string, object>();
                props.Add(QUEUE_NAME, queueInfo.Name);
                props.Add(QUEUE_MESSAGE_COUNT, queueInfo.MessageCount);
                props.Add(QUEUE_CONSUMER_COUNT, queueInfo.ConsumerCount);
                return props;
            }
            else
            {
                return null;
            }
        }

        public void PurgeQueue(string queueName, bool noWait)
        {
            if (noWait)
            {
                Task.Run(() => PurgeQueue(queueName));
            }
            else
            {
                PurgeQueue(queueName);
            }
        }

        public uint PurgeQueue(string queueName)
        {
            return RabbitTemplate.Execute(
                channel =>
                {
                    var queuePurged = channel.QueuePurge(queueName);
                    _logger?.LogDebug("Purged queue: " + queueName + ", " + queuePurged);
                    return queuePurged;
                });
        }

        public void RemoveBinding(Binding binding)
        {
            RabbitTemplate.Execute<object>(
                channel =>
                {
                    if (binding.IsDestinationQueue)
                    {
                        if (IsRemovingImplicitQueueBinding(binding))
                        {
                            return null;
                        }

                        channel.QueueUnbind(binding.Destination, binding.Exchange, binding.RoutingKey, binding.Arguments);
                    }
                    else
                    {
                        channel.ExchangeUnbind(binding.Destination, binding.Exchange, binding.RoutingKey, binding.Arguments);
                    }

                    return null;
                });
        }

        #region IConnectionListener
        public void OnCreate(Connection.IConnection connection)
        {
            _logger?.LogDebug("OnCreate for connection: ", connection?.ToString());

            if (Interlocked.CompareExchange(ref _initializing, 1, 0) != 0)
            {
                return;
            }

            try
            {
                /*
                 * ...but it is possible for this to happen twice in the same ConnectionFactory (if more than
                 * one concurrent Connection is allowed). It's idempotent, so no big deal (a bit of network
                 * chatter). In fact it might even be a good thing: exclusive queues only make sense if they are
                 * declared for every connection. If anyone has a problem with it: use auto-startup="false".
                 */
                if (RetryTemplate != null && !RetryDisabled)
                {
                    RetryTemplate.Execute(
                        c =>
                        {
                            Initialize();
                        });
                }
                else
                {
                    Initialize();
                }
            }
            finally
            {
                Interlocked.CompareExchange(ref _initializing, 0, 1);
            }
        }

        public void OnClose(Connection.IConnection connection)
        {
            _logger?.LogDebug("OnClose for connection: ", connection?.ToString());
        }

        public void OnShutDown(ShutdownEventArgs args)
        {
            _logger?.LogDebug("OnShutDown for connection: ", args.ToString());
        }
        #endregion

        public void Initialize()
        {
            _logger?.LogDebug("Initializing declarations");
            if (ApplicationContext == null)
            {
                _logger?.LogDebug("No ApplicationContext has been set, cannot auto-declare Exchanges, Queues, and Bindings");
                return;
            }

            var contextExchanges = ApplicationContext.GetServices<IExchange>().ToList();
            var contextQueues = ApplicationContext.GetServices<Queue>().ToList();
            var contextBindings = ApplicationContext.GetServices<Binding>().ToList();
            var customizers = ApplicationContext.GetServices<IDeclarableCustomizer>().ToList();

            ProcessDeclarables(contextExchanges, contextQueues, contextBindings);

            var exchanges = FilterDeclarables(contextExchanges, customizers);
            var queues = FilterDeclarables(contextQueues, customizers);
            var bindings = FilterDeclarables(contextBindings, customizers);

            foreach (var exchange in exchanges)
            {
                if (!exchange.IsDurable || exchange.IsAutoDelete)
                {
                    _logger?.LogInformation("Auto-declaring a non-durable or auto-delete Exchange ("
                            + exchange.Name
                            + ") durable:" + exchange.IsDurable + ", auto-delete:" + exchange.IsAutoDelete + ". "
                            + "It will be deleted by the broker if it shuts down, and can be redeclared by closing and "
                            + "reopening the connection.");
                }
            }

            foreach (var queue in queues)
            {
                if (!queue.Durable || queue.AutoDelete || queue.Exclusive)
                {
                    _logger?.LogInformation("Auto-declaring a non-durable, auto-delete, or exclusive Queue ("
                            + queue.Name
                            + ") durable:" + queue.Durable + ", auto-delete:" + queue.AutoDelete + ", exclusive:"
                            + queue.Exclusive + ". "
                            + "It will be redeclared if the broker stops and is restarted while the connection factory is "
                            + "alive, but all messages will be lost.");
                }
            }

            if (exchanges.Count == 0 && queues.Count == 0 && bindings.Count == 0)
            {
                _logger?.LogDebug("Nothing to declare");
                return;
            }

            RabbitTemplate.Execute<object>(
                channel =>
                {
                    DeclareExchanges(channel, exchanges.ToArray());
                    DeclareQueues(channel, queues.ToArray());
                    DeclareBindings(channel, bindings.ToArray());
                    return null;
                });

            _logger?.LogDebug("Declarations finished");
        }

        private void ProcessDeclarables(List<IExchange> contextExchanges, List<Queue> contextQueues, List<Binding> contextBindings)
        {
            var declarables = ApplicationContext.GetServices<Declarables>();
            foreach (var declarable in declarables)
            {
                foreach (var d in declarable.DeclarableList)
                {
                    if (d is IExchange)
                    {
                        contextExchanges.Add((IExchange)d);
                    }
                    else if (d is Queue)
                    {
                        contextQueues.Add((Queue)d);
                    }
                    else if (d is Binding)
                    {
                        contextBindings.Add((Binding)d);
                    }
                }
            }
        }

        private List<T> FilterDeclarables<T>(IEnumerable<T> declarables, IEnumerable<IDeclarableCustomizer> customizers)
            where T : IDeclarable
        {
            var results = new List<T>();
            var customizerCounts = customizers.Count();
            foreach (var dec in declarables)
            {
                if (ShouldDeclare(dec))
                {
                    if (customizerCounts == 0)
                    {
                        results.Add(dec);
                    }

                    foreach (var customizer in customizers)
                    {
                        var result = customizer.Apply(dec);
                        if (result != null)
                        {
                            results.Add((T)result);
                        }
                    }
                }
            }

            return results;
        }

        private bool ShouldDeclare<T>(T declarable)
            where T : IDeclarable
        {
            if (!declarable.Declare)
            {
                return false;
            }

            return (declarable.Admins.Count == 0 && !ExplicitDeclarationsOnly)
                    || declarable.Admins.Contains(this)
                    || (Name != null && declarable.Admins.Contains(Name));
        }

        private void DeclareExchanges(IModel channel, params IExchange[] exchanges)
        {
            foreach (var exchange in exchanges)
            {
                _logger?.LogDebug("declaring Exchange '" + exchange.Name + "'");

                if (!IsDeclaringDefaultExchange(exchange))
                {
                    try
                    {
                        if (exchange.IsDelayed)
                        {
                            var arguments = exchange.Arguments;
                            if (arguments == null)
                            {
                                arguments = new Dictionary<string, object>();
                            }
                            else
                            {
                                arguments = new Dictionary<string, object>(arguments);
                            }

                            arguments["x-delayed-type"] = exchange.Type;

                            // TODO: exchange.IsInternal
                            channel.ExchangeDeclare(exchange.Name, DELAYED_MESSAGE_EXCHANGE, exchange.IsDurable, exchange.IsAutoDelete, arguments);
                        }
                        else
                        {
                            // TODO: exchange.IsInternal
                            channel.ExchangeDeclare(exchange.Name, exchange.Type, exchange.IsDurable, exchange.IsAutoDelete, exchange.Arguments);
                        }
                    }
                    catch (IOException e)
                    {
                        LogOrRethrowDeclarationException(exchange, "exchange", e);
                    }
                }
            }
        }

        private List<QueueDeclareOk> DeclareQueues(IModel channel, params Queue[] queues)
        {
            var declareOks = new List<QueueDeclareOk>(queues.Length);
            for (var i = 0; i < queues.Length; i++)
            {
                var queue = queues[i];
                if (!queue.Name.StartsWith("amq."))
                {
                    _logger?.LogDebug("declaring Queue '" + queue.Name + "'");

                    if (queue.Name.Length > 255)
                    {
                        throw new ArgumentException("Queue names limited to < 255 characters");
                    }

                    try
                    {
                        try
                        {
                            var declareOk = channel.QueueDeclare(queue.Name, queue.Durable, queue.Exclusive, queue.AutoDelete, queue.Arguments);
                            if (!string.IsNullOrEmpty(declareOk.QueueName))
                            {
                                queue.ActualName = declareOk.QueueName;
                            }

                            declareOks.Add(declareOk);
                        }
                        catch (Exception e)
                        {
                            CloseChannelAfterIllegalArg(channel, queue);
                            throw new IOException(string.Empty, e);
                        }
                    }
                    catch (IOException e)
                    {
                        LogOrRethrowDeclarationException(queue, "queue", e);
                    }
                }
                else
                {
                    _logger?.LogDebug(queue.Name + ": Queue with name that starts with 'amq.' cannot be declared.");
                }
            }

            return declareOks;
        }

        private void CloseChannelAfterIllegalArg(IModel channel, Queue queue)
        {
            _logger?.LogError("Exception while declaring queue: '" + queue.Name + "'");
            try
            {
                if (channel is IChannelProxy)
                {
                    ((IChannelProxy)channel).TargetChannel.Close();
                }
            }
            catch (Exception e1)
            {
                _logger?.LogError("Failed to close channel after illegal argument", e1);
            }
        }

        private void DeclareBindings(IModel channel, params Binding[] bindings)
        {
            foreach (var binding in bindings)
            {
                _logger?.LogDebug("Binding destination [" + binding.Destination + " (" + binding.Type
                        + ")] to exchange [" + binding.Exchange + "] with routing key [" + binding.RoutingKey + "]");

                try
                {
                    if (binding.IsDestinationQueue)
                    {
                        if (!IsDeclaringImplicitQueueBinding(binding))
                        {
                            channel.QueueBind(binding.Destination, binding.Exchange, binding.RoutingKey, binding.Arguments);
                        }
                    }
                    else
                    {
                        channel.ExchangeBind(binding.Destination, binding.Exchange, binding.RoutingKey, binding.Arguments);
                    }
                }
                catch (IOException e)
                {
                    LogOrRethrowDeclarationException(binding, "binding", e);
                }
            }
        }

        private void LogOrRethrowDeclarationException(IDeclarable element, string elementType, Exception exception)
        {
            if (IgnoreDeclarationExceptions || (element != null && element.IgnoreDeclarationExceptions))
            {
                _logger?.LogDebug("Failed to declare " + elementType + ": " + (element == null ? "broker-generated" : element.ToString()) + ", continuing...", exception);

                var cause = exception;
                if (exception is IOException && exception.InnerException != null)
                {
                    cause = exception.InnerException;
                }

                _logger?.LogWarning("Failed to declare " + elementType + ": " + (element == null ? "broker-generated" : element.ToString()) + ", continuing... " + cause);
            }
            else
            {
                throw exception;
            }
        }

        private bool IsDeclaringDefaultExchange(IExchange exchange)
        {
            if (IsDefaultExchange(exchange.Name))
            {
                _logger?.LogDebug("Default exchange is pre-declared by server.");
                return true;
            }

            return false;
        }

        private bool IsDeletingDefaultExchange(string exchangeName)
        {
            if (IsDefaultExchange(exchangeName))
            {
                _logger?.LogDebug("Default exchange cannot be deleted.");
                return true;
            }

            return false;
        }

        private bool IsDefaultExchange(string exchangeName)
        {
            return exchangeName == string.Empty;
        }

        private bool IsDeclaringImplicitQueueBinding(Binding binding)
        {
            if (IsImplicitQueueBinding(binding))
            {
                _logger?.LogDebug("The default exchange is implicitly bound to every queue, with a routing key equal to the queue name.");
                return true;
            }

            return false;
        }

        private bool IsRemovingImplicitQueueBinding(Binding binding)
        {
            if (IsImplicitQueueBinding(binding))
            {
                _logger?.LogDebug("Cannot remove implicit default exchange binding to queue.");
                return true;
            }

            return false;
        }

        private bool IsImplicitQueueBinding(Binding binding)
        {
            return IsDefaultExchange(binding.Exchange) && binding.Destination.Equals(binding.RoutingKey);
        }

        private void DoInitialize()
        {
            lock (_lifecycleMonitor)
            {
                if (IsRunning || !IsAutoStartup)
                {
                    return;
                }

                if (RetryTemplate == null && !RetryDisabled)
                {
                    RetryTemplate = new PollyRetryTemplate(new Dictionary<Type, bool>(), DECLARE_MAX_ATTEMPTS, true, DECLARE_INITIAL_RETRY_INTERVAL, DECLARE_MAX_RETRY_INTERVAL, DECLARE_RETRY_MULTIPLIER);
                }

                if (ConnectionFactory is CachingConnectionFactory && ((CachingConnectionFactory)ConnectionFactory).CacheMode == CachingMode.CONNECTION)
                {
                    _logger?.LogWarning("RabbitAdmin auto declaration is not supported with CacheMode.CONNECTION");
                    return;
                }

                ConnectionFactory.AddConnectionListener(this);
                IsRunning = true;
            }
        }
    }
}
