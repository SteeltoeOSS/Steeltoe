// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Stream.Binder.Rabbit.Config;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Provisioning;
using System;
using System.Collections.Generic;
using System.Linq;
using static Steeltoe.Messaging.RabbitMQ.Config.Binding;
using RabbitConfig = Steeltoe.Messaging.RabbitMQ.Config;

namespace Steeltoe.Stream.Binder.Rabbit.Provisioning
{
    public class RabbitExchangeQueueProvisioner : IProvisioningProvider
    {
        private const string GROUP_INDEX_DELIMITER = ".";
        private readonly IApplicationContext _autoDeclareContext;
        private readonly ILogger _logger;

        private sealed class GivenNamingStrategy : INamingStrategy
        {
            private readonly Func<string> _strategy;

            public GivenNamingStrategy(Func<string> strategy)
            {
                _strategy = strategy;
            }

            public string GenerateName()
            {
                return _strategy();
            }
        }

        public RabbitExchangeQueueProvisioner(IConnectionFactory connectionFactory, IOptionsMonitor<RabbitBindingsOptions> bindingsOptions, IApplicationContext applicationContext, ILogger<RabbitExchangeQueueProvisioner> logger)
            : this(connectionFactory, new List<IDeclarableCustomizer>(), bindingsOptions, applicationContext, logger)
        {
        }

        public RabbitExchangeQueueProvisioner(IConnectionFactory connectionFactory, List<IDeclarableCustomizer> customizers, IOptionsMonitor<RabbitBindingsOptions> bindingsOptions, IApplicationContext applicationContext, ILogger<RabbitExchangeQueueProvisioner> logger)
        {
            Admin = new RabbitAdmin(applicationContext, connectionFactory, logger);

            _autoDeclareContext = applicationContext;
            _logger = logger;
            Admin.ApplicationContext = _autoDeclareContext;
            Admin.Initialize();
            Customizers = customizers;
            Options = bindingsOptions.CurrentValue;
        }

        private RabbitAdmin Admin { get; }

        private bool _notOurAdminException = true; // Should be set by onApplicationEvent

        private List<IDeclarableCustomizer> Customizers { get; }

        private RabbitBindingsOptions Options { get; }

        public static string ConstructDLQName(string name)
        {
            return $"{name}.dlq";
        }

        public static string ApplyPrefix(string prefix, string name)
        {
            return prefix + name;
        }

        public IProducerDestination ProvisionProducerDestination(string name, IProducerOptions options)
        {
            var producerProperties = Options.GetRabbitProducerOptions(options.BindingName);

            var exchangeName = ApplyPrefix(producerProperties.Prefix, name);
            var exchange = BuildExchange(producerProperties, exchangeName);
            if (producerProperties.DeclareExchange.Value)
            {
                DeclareExchange(exchangeName, exchange);
            }

            RabbitConfig.IBinding binding = null;
            foreach (var requiredGroupName in options.RequiredGroups)
            {
                var baseQueueName = producerProperties.QueueNameGroupOnly.Value ? requiredGroupName : $"{exchangeName}.{requiredGroupName}";
                if (!options.IsPartitioned)
                {
                    AutoBindDLQ(baseQueueName, baseQueueName, producerProperties);
                    if (producerProperties.BindQueue.Value)
                    {
                        var queue = new Queue(baseQueueName, true, false, false, GetQueueArgs(baseQueueName, producerProperties, false));
                        DeclareQueue(baseQueueName, queue);
                        var routingKeys = BindingRoutingKeys(producerProperties);
                        if (routingKeys == null || routingKeys.Count == 0)
                        {
                            binding = NotPartitionedBinding(exchange, queue, null, producerProperties);
                        }
                        else
                        {
                            foreach (var routingKey in routingKeys)
                            {
                                binding = NotPartitionedBinding(exchange, queue, routingKey, producerProperties);
                            }
                        }
                    }
                }
                else
                {
                    // if the stream is partitioned, create one queue for each target partition for the default group
                    for (var i = 0; i < options.PartitionCount; i++)
                    {
                        var partitionSuffix = $"-{i}";
                        var partitionQueueName = baseQueueName + partitionSuffix;
                        AutoBindDLQ(baseQueueName, baseQueueName + partitionSuffix, producerProperties);
                        if (producerProperties.BindQueue.Value)
                        {
                            var queue = new Queue(partitionQueueName, true, false, false, GetQueueArgs(partitionQueueName, producerProperties, false));
                            DeclareQueue(queue.QueueName, queue);
                            var prefix = producerProperties.Prefix;
                            var destination = string.IsNullOrEmpty(prefix) ? exchangeName : exchangeName.Substring(prefix.Length);
                            var routingKeys = BindingRoutingKeys(producerProperties);
                            if (routingKeys == null || routingKeys.Count == 0)
                            {
                                binding = PartitionedBinding(destination, exchange, queue, null, producerProperties, i);
                            }
                            else
                            {
                                foreach (var routingKey in routingKeys)
                                {
                                    binding = PartitionedBinding(destination, exchange, queue, routingKey, producerProperties, i);
                                }
                            }
                        }
                    }
                }
            }

            return new RabbitProducerDestination(exchange, binding);
        }

        public IConsumerDestination ProvisionConsumerDestination(string name, string group, IConsumerOptions options)
        {
            var consumerProperties = Options.GetRabbitConsumerOptions(options.BindingName);
            IConsumerDestination consumerDestination;
            if (!options.Multiplex)
            {
                consumerDestination = DoProvisionConsumerDestination(name, group, options);
            }
            else
            {
                var consumerDestinationNames = new List<string>();
                var trimmed = name.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());
                foreach (var destination in trimmed)
                {
                    if (options.IsPartitioned && options.InstanceIndexList.Count > 0)
                    {
                        foreach (var index in options.InstanceIndexList)
                        {
                            var temporaryOptions = options.Clone() as ConsumerOptions;
                            temporaryOptions.InstanceIndex = index;
                            consumerDestinationNames.Add(DoProvisionConsumerDestination(destination, group, temporaryOptions).Name);
                        }
                    }
                    else
                    {
                        consumerDestinationNames.Add(DoProvisionConsumerDestination(destination, group, options).Name);
                    }
                }

                consumerDestination = new RabbitConsumerDestination(string.Join(',', consumerDestinationNames), null);
            }

            return consumerDestination;
        }

        public void CleanAutoDeclareContext(IConsumerDestination destination, IConsumerOptions consumerProperties)
        {
            lock (_autoDeclareContext)
            {
                destination.Name.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(name =>
                {
                    name = name.Trim();
                    RemoveSingleton($"{name}.binding");
                    RemoveSingleton(name);
                    var dlq = $"{name}.dlq";
                    RemoveSingleton($"{dlq}.binding");
                    RemoveSingleton(dlq);
                });
            }
        }

        protected virtual string GetGroupedName(string name, string group)
        {
            return name + GROUP_INDEX_DELIMITER + (!string.IsNullOrEmpty(group) ? group : "default");
        }

        private IConsumerDestination DoProvisionConsumerDestination(string name, string group, IConsumerOptions options)
        {
            var consumerProperties = Options.GetRabbitConsumerOptions(options.BindingName);
            var anonymous = string.IsNullOrEmpty(group);
            Base64UrlNamingStrategy anonQueueNameGenerator = null;
            if (anonymous)
            {
                anonQueueNameGenerator = new Base64UrlNamingStrategy(consumerProperties.AnonymousGroupPrefix ?? string.Empty);
            }

            string baseQueueName;
            if (consumerProperties.QueueNameGroupOnly.GetValueOrDefault())
            {
                baseQueueName = anonymous ? anonQueueNameGenerator.GenerateName() : group;
            }
            else
            {
                baseQueueName = GetGroupedName(name, anonymous ? anonQueueNameGenerator.GenerateName() : group);
            }

            // logger.info("declaring queue for inbound: " + baseQueueName + ", bound to: " + name);
            var prefix = consumerProperties.Prefix;
            var exchangeName = ApplyPrefix(prefix, name);
            var exchange = BuildExchange(consumerProperties, exchangeName);
            if (consumerProperties.DeclareExchange.GetValueOrDefault())
            {
                DeclareExchange(exchangeName, exchange);
            }

            var queueName = ApplyPrefix(prefix, baseQueueName);
            var partitioned = !anonymous && options.IsPartitioned;
            var durable = !anonymous && consumerProperties.DurableSubscription.Value;
            Queue queue;
            if (anonymous)
            {
                var anonQueueName = queueName;
                queue = new AnonymousQueue(new GivenNamingStrategy(() => anonQueueName), GetQueueArgs(queueName, consumerProperties, false));
            }
            else
            {
                if (partitioned)
                {
                    var partitionSuffix = $"-{options.InstanceIndex}";
                    queueName += partitionSuffix;
                }

                queue = durable
                    ? new Queue(queueName, true, false, false, GetQueueArgs(queueName, consumerProperties, false))
                    : new Queue(queueName, false, false, true, GetQueueArgs(queueName, consumerProperties, false));
            }

            RabbitConfig.IBinding binding = null;
            if (consumerProperties.BindQueue.GetValueOrDefault())
            {
                DeclareQueue(queueName, queue);
                var routingKeys = BindingRoutingKeys(consumerProperties);
                if (routingKeys == null || routingKeys.Count == 0)
                {
                    binding = DeclareConsumerBindings(name, null, options, exchange, partitioned, queue);
                }
                else
                {
                    foreach (var routingKey in routingKeys)
                    {
                        binding = DeclareConsumerBindings(name, routingKey, options, exchange, partitioned, queue);
                    }
                }
            }

            if (durable)
            {
                AutoBindDLQ(ApplyPrefix(consumerProperties.Prefix, baseQueueName), queueName, consumerProperties);
            }

            return new RabbitConsumerDestination(queue.QueueName, binding);
        }

        private RabbitConfig.IBinding DeclareConsumerBindings(
            string name,
            string routingKey,
            IConsumerOptions options,
            IExchange exchange,
            bool partitioned,
            Queue queue)
        {
            var consumerProperties = Options.GetRabbitConsumerOptions(options.BindingName);
            if (partitioned)
            {
                return PartitionedBinding(name, exchange, queue, routingKey, consumerProperties, options.InstanceIndex);
            }
            else
            {
                return NotPartitionedBinding(exchange, queue, routingKey, consumerProperties);
            }
        }

        private RabbitConfig.IBinding PartitionedBinding(
            string destination,
            IExchange exchange,
            Queue queue,
            string rk,
            RabbitCommonOptions extendedProperties,
            int index)
        {
            var bindingKey = rk ?? destination;

            bindingKey += $"-{index}";
            var arguments = new Dictionary<string, object>();
            foreach (var entry in extendedProperties.QueueBindingArguments)
            {
                arguments.Add(entry.Key, entry.Value);
            }

            switch (exchange)
            {
                case TopicExchange topic:
                    {
                        var binding = BindingBuilder.Bind(queue).To(topic).With(bindingKey);
                        DeclareBinding(queue.QueueName, binding);
                        return binding;
                    }

                case DirectExchange direct:
                    {
                        var binding = BindingBuilder.Bind(queue).To(direct).With(bindingKey);
                        DeclareBinding(queue.QueueName, binding);
                        return binding;
                    }

                case FanoutExchange:
                    throw new ProvisioningException("A fanout exchange is not appropriate for partitioned apps");
                case HeadersExchange:
                    {
                        var binding = new RabbitConfig.Binding($"{queue.QueueName}.{exchange.ExchangeName}.binding", queue.QueueName, DestinationType.QUEUE, exchange.ExchangeName, string.Empty, arguments);
                        DeclareBinding(queue.QueueName, binding);
                        return binding;
                    }

                default:
                    throw new ProvisioningException($"Cannot bind to a {exchange.Type} exchange");
            }
        }

        private RabbitConfig.IBinding NotPartitionedBinding(
            IExchange exchange,
            Queue queue,
            string rk,
            RabbitCommonOptions extendedProperties)
        {
            var routingKey = rk ?? "#";

            var arguments = new Dictionary<string, object>();
            foreach (var entry in extendedProperties.QueueBindingArguments)
            {
                arguments.Add(entry.Key, entry.Value);
            }

            switch (exchange)
            {
                case TopicExchange topic:
                    {
                        var binding = BindingBuilder.Bind(queue).To(topic).With(routingKey);
                        DeclareBinding(queue.QueueName, binding);
                        return binding;
                    }

                case DirectExchange direct:
                    {
                        var binding = BindingBuilder.Bind(queue).To(direct).With(routingKey);
                        DeclareBinding(queue.QueueName, binding);
                        return binding;
                    }

                case FanoutExchange fanout:
                    {
                        var binding = BindingBuilder.Bind(queue).To(fanout);
                        DeclareBinding(queue.QueueName, binding);
                        return binding;
                    }

                case HeadersExchange:
                    {
                        var binding = new RabbitConfig.Binding($"{queue.QueueName}.{exchange.ExchangeName}.binding", queue.QueueName, DestinationType.QUEUE, exchange.ExchangeName, string.Empty, arguments);
                        DeclareBinding(queue.QueueName, binding);
                        return binding;
                    }

                default:
                    throw new ProvisioningException($"Cannot bind to a {exchange.Type} exchange");
            }
        }

        private List<string> BindingRoutingKeys(RabbitCommonOptions extendedProperties)
        {
            /*
             * When the delimiter is null, we get a String[1] containing the original.
             */
            var delimeter = extendedProperties.BindingRoutingKeyDelimiter;
            if (delimeter == null)
            {
                if (extendedProperties.BindingRoutingKey == null)
                {
                    return null;
                }

                return new List<string> { extendedProperties.BindingRoutingKey.Trim() };
            }

            if (extendedProperties.BindingRoutingKey == null)
            {
                return null;
            }

            var trimmed = extendedProperties.BindingRoutingKey.Split(delimeter, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());
            return new List<string>(trimmed);
        }

        private void AutoBindDLQ(string baseQueueName, string routingKey, RabbitCommonOptions properties)
        {
            var autoBindDlq = properties.AutoBindDlq.Value;

            _logger.LogDebug("autoBindDLQ=" + autoBindDlq + " for: " + baseQueueName);
            if (autoBindDlq)
            {
                var dlqName = properties.DeadLetterQueueName ?? ConstructDLQName(baseQueueName);

                var dlq = new Queue(dlqName, true, false, false, GetQueueArgs(dlqName, properties, true));
                DeclareQueue(dlqName, dlq);
                var dlxName = GetDeadLetterExchangeName(properties);
                if (properties.DeclareDlx.Value)
                {
                    DeclareExchange(dlxName, new ExchangeBuilder(dlxName, properties.DeadLetterExchangeType).Durable(true).Build());
                }

                var arguments = new Dictionary<string, object>();

                properties.DlqBindingArguments?.ToList().
                    ForEach(entry => arguments.Add(entry.Key, entry.Value));

                var dlRoutingKey = properties.DeadLetterRoutingKey ?? routingKey;
                var dlBindingName = $"{dlq.QueueName}.{dlxName}.{dlRoutingKey}.binding";
                var dlqBinding = new RabbitConfig.Binding(dlBindingName, dlq.QueueName, DestinationType.QUEUE, dlxName, dlRoutingKey, arguments);
                DeclareBinding(dlqName, dlqBinding);
                if (properties is RabbitConsumerOptions options && options.RepublishToDlq.Value)
                {
                    /*
                     * Also bind with the base queue name when republishToDlq is used, which does not know about partitioning
                     */
                    var bindingName = $"{dlq.QueueName}.{dlxName}.{baseQueueName}.binding";
                    DeclareBinding(dlqName, new RabbitConfig.Binding(bindingName, dlq.QueueName, DestinationType.QUEUE, dlxName, baseQueueName, arguments));
                }
            }
        }

        private string GetDeadLetterExchangeName(RabbitCommonOptions properties)
        {
            if (properties.DeadLetterExchange == null)
            {
                return properties.Prefix + RabbitCommonOptions.DEAD_LETTER_EXCHANGE;
            }
            else
            {
                return properties.DeadLetterExchange;
            }
        }

        private void DeclareQueue(string beanName, Queue queueArg)
        {
            var queue = queueArg;
            foreach (var customizer in Customizers)
            {
                queue = (Queue)customizer.Apply(queue);
            }

            try
            {
                Admin.DeclareQueue(queue);
            }
            catch (RabbitConnectException e)
            {
                _logger.LogDebug("Declaration of queue: " + queue.QueueName + " deferred - connection not available " + e.Message);
            }
            catch (Exception e)
            {
                if (_notOurAdminException)
                {
                    _notOurAdminException = false;
                    throw;
                }

                _logger.LogDebug("Declaration of queue: " + queue.QueueName + " deferred", e);
            }

            AddToAutoDeclareContext(beanName, queue);
        }

        private Dictionary<string, object> GetQueueArgs(string queueName, RabbitCommonOptions properties, bool isDlq)
        {
            var args = new Dictionary<string, object>();
            if (!isDlq)
            {
                if (properties.AutoBindDlq.Value)
                {
                    var dlx = properties.DeadLetterExchange ?? ApplyPrefix(properties.Prefix, "DLX");

                    args.Add("x-dead-letter-exchange", dlx);

                    var dlRk = properties.DeadLetterRoutingKey ?? queueName;

                    args.Add("x-dead-letter-routing-key", dlRk);
                }
            }
            else
            {
                if (properties.DlqDeadLetterExchange != null)
                {
                    args.Add("x-dead-letter-exchange", properties.DlqDeadLetterExchange);
                }

                if (properties.DlqDeadLetterRoutingKey != null)
                {
                    args.Add("x-dead-letter-routing-key", properties.DlqDeadLetterRoutingKey);
                }
            }

            AddAdditionalArgs(args, properties, isDlq);
            return args;
        }

        private void AddAdditionalArgs(Dictionary<string, object> args, RabbitCommonOptions properties, bool isDlq)
        {
            var expires = isDlq ? properties.DlqExpires : properties.Expires;
            var maxLength = isDlq ? properties.DlqMaxLength : properties.MaxLength;
            var maxLengthBytes = isDlq ? properties.DlqMaxLengthBytes : properties.MaxLengthBytes;
            var maxPriority = isDlq ? properties.DlqMaxPriority : properties.MaxPriority;
            var ttl = isDlq ? properties.DlqTtl : properties.Ttl;
            var lazy = isDlq ? properties.DlqLazy : properties.Lazy;
            var overflow = isDlq ? properties.DlqOverflowBehavior : properties.OverflowBehavior;
            var quorum = isDlq ? properties.DlqQuorum : properties.Quorum;
            var singleActive = isDlq ? properties.DlqSingleActiveConsumer : properties.SingleActiveConsumer;

            if (expires != null)
            {
                args.Add("x-expires", expires.Value);
            }

            if (maxLength != null)
            {
                args.Add("x-max-length", maxLength.Value);
            }

            if (maxLengthBytes != null)
            {
                args.Add("x-max-length-bytes", maxLengthBytes.Value);
            }

            if (maxPriority != null)
            {
                args.Add("x-max-priority", maxPriority.Value);
            }

            if (ttl != null)
            {
                args.Add("x-message-ttl", ttl.Value);
            }

            if (lazy.GetValueOrDefault())
            {
                args.Add("x-queue-mode", "lazy");
            }

            if (!string.IsNullOrEmpty(overflow))
            {
                args.Add("x-overflow", overflow);
            }

            if (quorum != null && quorum.Enabled.Value)
            {
                args.Add("x-queue-type", "quorum");
                if (quorum.DeliveryLimit != null)
                {
                    args.Add("x-delivery-limit", quorum.DeliveryLimit.Value);
                }

                if (quorum.InitialQuorumSize != null)
                {
                    args.Add("x-quorum-initial-group-size", quorum.InitialQuorumSize.Value);
                }
            }

            if (singleActive.GetValueOrDefault())
            {
                args.Add("x-single-active-consumer", true);
            }
        }

        private IExchange BuildExchange(RabbitCommonOptions properties, string exchangeName)
        {
            try
            {
                var builder = new ExchangeBuilder(exchangeName, properties.ExchangeType);
                builder.Durable(properties.ExchangeDurable.GetValueOrDefault());
                if (properties.ExchangeAutoDelete.GetValueOrDefault())
                {
                    builder.AutoDelete();
                }

                if (properties.DelayedExchange.GetValueOrDefault())
                {
                    builder.Delayed();
                }

                return builder.Build();
            }
            catch (Exception e)
            {
                throw new ProvisioningException("Failed to create exchange object", e);
            }
        }

        private void DeclareExchange(string rootName, IExchange exchangeArg)
        {
            var exchange = exchangeArg;
            foreach (var customizer in Customizers)
            {
                exchange = (IExchange)customizer.Apply(exchange);
            }

            try
            {
                Admin.DeclareExchange(exchange);
            }
            catch (RabbitConnectException e)
            {
                _logger?.LogDebug("Declaration of exchange: " + exchange.ExchangeName + " deferred - connection not available; " + e.Message);
            }
            catch (Exception e)
            {
                if (_notOurAdminException)
                {
                    _notOurAdminException = false;
                    throw;
                }

                _logger.LogDebug("Declaration of exchange: " + exchange.ExchangeName + " deferred", e);
            }

            AddToAutoDeclareContext($"{rootName}.exchange", exchange);
        }

        private void AddToAutoDeclareContext(string name, object bean)
        {
            lock (_autoDeclareContext)
            {
                if (!_autoDeclareContext.ContainsService(name, bean.GetType()))
                {
                    _autoDeclareContext.Register(name, bean);
                }
            }
        }

        private void DeclareBinding(string rootName, RabbitConfig.IBinding bindingArg)
        {
            var binding = bindingArg;
            foreach (var customizer in Customizers)
            {
                binding = (RabbitConfig.IBinding)customizer.Apply(binding);
            }

            try
            {
                Admin.DeclareBinding(binding);
            }
            catch (RabbitConnectException e)
            {
                _logger.LogDebug("Declaration of binding: " + rootName + ".binding deferred - connection not available; " + e.Message);
            }
            catch (Exception e)
            {
                if (_notOurAdminException)
                {
                    _notOurAdminException = false;
                    throw;
                }

                _logger.LogDebug("Declaration of binding: " + rootName + ".binding deferred", e);
            }

            AddToAutoDeclareContext($"{rootName}.binding", binding);
        }

        private void RemoveSingleton(string name)
        {
            if (_autoDeclareContext.ContainsService(name))
            {
                _autoDeclareContext.Deregister(name);
            }
        }

        // public void onApplicationEvent(DeclarationExceptionEvent event)
        // {
        //    this.notOurAdminException = true; // our admin doesn't have an event publisher
        // }
        private sealed class RabbitProducerDestination : IProducerDestination
        {
            public IExchange Exchange { get; }

            public string Name => Exchange.ExchangeName;

            public RabbitConfig.IBinding Binding { get; }

            public RabbitProducerDestination(IExchange exchange, RabbitConfig.IBinding binding)
            {
                Exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
                Binding = binding;
            }

            public string GetNameForPartition(int partition)
            {
                return Exchange.ExchangeName;
            }

            public override string ToString()
            {
                return $"RabbitProducerDestination{{exchange={Exchange}, binding={Binding}}}";
            }
        }

        private sealed class RabbitConsumerDestination : IConsumerDestination
        {
            public string Name { get; }

            public RabbitConfig.IBinding Binding { get; }

            public RabbitConsumerDestination(string queueName, RabbitConfig.IBinding binding)
            {
                Name = queueName ?? throw new ArgumentNullException(nameof(queueName));
                Binding = binding;
            }

            public override string ToString()
            {
                return $"RabbitConsumerDestination{{queue={Name}, binding={Binding}}}";
            }
        }
    }
}
