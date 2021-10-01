// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Configuration;
using Steeltoe.Messaging.RabbitMQ.Attributes;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Messaging.RabbitMQ.Config
{
    public static class RabbitListenerDeclareAtrributeProcessor
    {
        private static readonly Dictionary<string, Queue> _queueDeclss = new Dictionary<string, Queue>();
        private static readonly Dictionary<string, QueueBinding> _bindingDecls = new Dictionary<string, QueueBinding>();

        internal static void ProcessDeclareAttributes(IServiceCollection services, IConfiguration configuration, Type targetClass)
        {
            if (targetClass == null)
            {
                throw new ArgumentNullException(nameof(targetClass));
            }

            var declareQueues = GetAllAttributes<DeclareQueueAttribute>(targetClass);
            var queues = ProcessDeclareQueues(services, configuration, declareQueues);
            UpdateQueueDeclarations(queues);

            var declareAnonQueues = GetAllAttributes<DeclareAnonymousQueueAttribute>(targetClass);
            var anonQueues = ProcessDeclareAnonymousQueues(services, configuration, declareAnonQueues);
            UpdateQueueDeclarations(anonQueues);

            var declareExchanges = GetAllAttributes<DeclareExchangeAttribute>(targetClass);
            ProcessDeclareExchanges(services, configuration, declareExchanges);

            var declareBindings = GetAllAttributes<DeclareQueueBindingAttribute>(targetClass);
            var bindings = ProcessDeclareQueueBindings(services, configuration, declareBindings);
            UpdateBindingDeclarations(bindings);
        }

        private static List<QueueBinding> ProcessDeclareQueueBindings(IServiceCollection services, IConfiguration config, List<DeclareQueueBindingAttribute> declareBindings)
        {
            var bindings = new List<QueueBinding>();
            foreach (var b in declareBindings)
            {
                var bindingName = PropertyPlaceholderHelper.ResolvePlaceholders(b.Name, config);

                if (b.RoutingKeys.Length > 0)
                {
                    foreach (var key in b.RoutingKeys)
                    {
                        var binding = BuildBinding(bindingName, b, config, key);
                        services.AddRabbitBinding(binding);
                        bindings.Add(binding);
                    }
                }
                else
                {
                    var binding = BuildBinding(bindingName, b, config);
                    services.AddRabbitBinding(binding);
                    bindings.Add(binding);
                }
            }

            return bindings;
        }

        private static List<IExchange> ProcessDeclareExchanges(IServiceCollection services, IConfiguration config, List<DeclareExchangeAttribute> declareExchanges)
        {
            var exchanges = new List<IExchange>();

            foreach (var e in declareExchanges)
            {
                var exchangeName = PropertyPlaceholderHelper.ResolvePlaceholders(e.Name, config);
                var exchange = CreateExchange(exchangeName, e.Type);
                if (!string.IsNullOrEmpty(e.Durable))
                {
                    exchange.IsDurable = GetBoolean(e.Durable, config, nameof(e.Durable));
                }

                if (!string.IsNullOrEmpty(e.Delayed))
                {
                    exchange.IsDelayed = GetBoolean(e.Delayed, config, nameof(e.Delayed));
                }

                if (!string.IsNullOrEmpty(e.AutoDelete))
                {
                    exchange.IsAutoDelete = GetBoolean(e.AutoDelete, config, nameof(e.AutoDelete));
                }

                if (!string.IsNullOrEmpty(e.IgnoreDeclarationExceptions))
                {
                    exchange.IgnoreDeclarationExceptions = GetBoolean(e.IgnoreDeclarationExceptions, config, nameof(e.IgnoreDeclarationExceptions));
                }

                if (!string.IsNullOrEmpty(e.Declare))
                {
                    exchange.ShouldDeclare = GetBoolean(e.Declare, config, nameof(e.Declare));
                }

                if (!string.IsNullOrEmpty(e.Internal))
                {
                    exchange.IsInternal = GetBoolean(e.Internal, config, nameof(e.Internal));
                }

                if (e.Admins.Length > 0)
                {
                    foreach (var a in e.Admins)
                    {
                        exchange.DeclaringAdmins.Add(a);
                    }
                }

                services.AddRabbitExchange(exchange);
                exchanges.Add(exchange);
            }

            return exchanges;
        }

        private static void UpdateBindingDeclarations(List<QueueBinding> bindings)
        {
            foreach (var binding in bindings)
            {
                _bindingDecls.TryAdd(binding.ServiceName, binding);
            }
        }

        private static void UpdateQueueDeclarations(List<Queue> queues)
        {
            foreach (var queue in queues)
            {
                _queueDeclss.TryAdd(queue.ServiceName, queue);
            }
        }

        private static QueueBinding BuildBinding(string bindingName, DeclareQueueBindingAttribute b, IConfiguration config, string routingKey = null)
        {
            var binding = new QueueBinding(bindingName);
            if (!string.IsNullOrEmpty(b.QueueName))
            {
                var reference = PropertyPlaceholderHelper.ResolvePlaceholders(b.QueueName, config);
                if (ConfigUtils.IsExpression(reference))
                {
                    reference = ConfigUtils.ExtractExpressionString(reference);
                    if (ConfigUtils.IsServiceReference(reference))
                    {
                        reference = ConfigUtils.ExtractServiceName(reference);
                    }
                }

                if (_queueDeclss.TryGetValue(reference, out var queueRef))
                {
                    reference = queueRef.QueueName;
                }

                binding.Destination = reference;
            }

            if (!string.IsNullOrEmpty(b.ExchangeName))
            {
                binding.Exchange = PropertyPlaceholderHelper.ResolvePlaceholders(b.ExchangeName, config);
            }
            else
            {
                binding.Exchange = string.Empty;
            }

            if (!string.IsNullOrEmpty(b.IgnoreDeclarationExceptions))
            {
                binding.IgnoreDeclarationExceptions = GetBoolean(b.IgnoreDeclarationExceptions, config, nameof(b.IgnoreDeclarationExceptions));
            }

            if (!string.IsNullOrEmpty(b.Declare))
            {
                binding.ShouldDeclare = GetBoolean(b.Declare, config, nameof(b.Declare));
            }

            if (!string.IsNullOrEmpty(routingKey))
            {
                binding.RoutingKey = PropertyPlaceholderHelper.ResolvePlaceholders(routingKey, config);
            }
            else
            {
                binding.RoutingKey = string.Empty;
            }

            if (b.Admins.Length > 0)
            {
                foreach (var a in b.Admins)
                {
                    binding.DeclaringAdmins.Add(a);
                }
            }

            return binding;
        }

        private static IExchange CreateExchange(string name, string type)
        {
            if (type == ExchangeType.DIRECT)
            {
                return new DirectExchange(name);
            }

            if (type == ExchangeType.FANOUT)
            {
                return new FanoutExchange(name);
            }

            if (type == ExchangeType.HEADERS)
            {
                return new HeadersExchange(name);
            }

            if (type == ExchangeType.TOPIC)
            {
                return new TopicExchange(name);
            }

            if (type == ExchangeType.SYSTEM)
            {
                return new CustomExchange(name, ExchangeType.SYSTEM);
            }

            throw new InvalidOperationException("Unable to determine exchange type " + type);
        }

        private static List<Queue> ProcessDeclareQueues(IServiceCollection services, IConfiguration config, List<DeclareQueueAttribute> declareQueues)
        {
            var queues = new List<Queue>();
            foreach (var q in declareQueues)
            {
                var queueName = PropertyPlaceholderHelper.ResolvePlaceholders(q.Name, config);
                var queue = new Queue(queueName);
                UpdateQueue(queue, q, config);
                services.AddRabbitQueue(queue);
                queues.Add(queue);
            }

            return queues;
        }

        private static List<Queue> ProcessDeclareAnonymousQueues(IServiceCollection services, IConfiguration config, List<DeclareAnonymousQueueAttribute> declareQueues)
        {
            var queues = new List<Queue>();
            foreach (var q in declareQueues)
            {
                var queue = new Queue(q.Name, false, true, true);
                queue.ServiceName = q.Id;
                UpdateQueue(queue, q, config);
                services.AddRabbitQueue(queue);
                queues.Add(queue);
            }

            return queues;
        }

        private static void UpdateQueue(Queue queue, DeclareQueueBase q, IConfiguration config)
        {
            if (!string.IsNullOrEmpty(q.Durable))
            {
                queue.IsDurable = GetBoolean(q.Durable, config, nameof(q.Durable));
            }

            if (!string.IsNullOrEmpty(q.Exclusive))
            {
                queue.IsExclusive = GetBoolean(q.Exclusive, config, nameof(q.Exclusive));
            }

            if (!string.IsNullOrEmpty(q.AutoDelete))
            {
                queue.IsAutoDelete = GetBoolean(q.AutoDelete, config, nameof(q.AutoDelete));
            }

            if (!string.IsNullOrEmpty(q.IgnoreDeclarationExceptions))
            {
                queue.IgnoreDeclarationExceptions = GetBoolean(q.IgnoreDeclarationExceptions, config, nameof(q.IgnoreDeclarationExceptions));
            }

            if (!string.IsNullOrEmpty(q.Declare))
            {
                queue.ShouldDeclare = GetBoolean(q.Declare, config, nameof(q.Declare));
            }

            if (q.Admins.Length > 0)
            {
                foreach (var a in q.Admins)
                {
                    queue.DeclaringAdmins.Add(a);
                }
            }
        }

        private static bool GetBoolean(string value, IConfiguration config, string name)
        {
            value = PropertyPlaceholderHelper.ResolvePlaceholders(value, config);
            if (bool.TryParse(value, out var result))
            {
                return result;
            }

            throw new InvalidOperationException("Unable to parse annotation property: " + name);
        }

        private static List<T> GetAllAttributes<T>(Type targetClass)
            where T : Attribute
        {
            var results = new List<T>();

            var classLevel = targetClass.GetCustomAttributes<T>();
            results.AddRange(classLevel);
            var reflectMethods = targetClass.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var m in reflectMethods)
            {
                var methodLevel = m.GetCustomAttributes<T>();
                results.AddRange(methodLevel);
            }

            return results;
        }
    }
}
