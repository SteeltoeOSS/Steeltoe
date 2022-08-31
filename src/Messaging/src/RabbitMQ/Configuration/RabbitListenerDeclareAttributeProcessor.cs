// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;
using Steeltoe.Common.Configuration;
using Steeltoe.Messaging.RabbitMQ.Attributes;
using Steeltoe.Messaging.RabbitMQ.Extensions;

namespace Steeltoe.Messaging.RabbitMQ.Configuration;

public static class RabbitListenerDeclareAttributeProcessor
{
    private static readonly Dictionary<string, Queue> QueueMap = new();
    private static readonly Dictionary<string, QueueBinding> BindingMap = new();

    internal static void ProcessDeclareAttributes(IServiceCollection services, IConfiguration configuration, Type targetClass)
    {
        ArgumentGuard.NotNull(targetClass);

        List<DeclareQueueAttribute> declareQueues = GetAllAttributes<DeclareQueueAttribute>(targetClass);
        List<Queue> queues = ProcessDeclareQueues(services, configuration, declareQueues);
        UpdateQueueDeclarations(queues);

        List<DeclareAnonymousQueueAttribute> declareAnonQueues = GetAllAttributes<DeclareAnonymousQueueAttribute>(targetClass);
        List<Queue> anonQueues = ProcessDeclareAnonymousQueues(services, configuration, declareAnonQueues);
        UpdateQueueDeclarations(anonQueues);

        List<DeclareExchangeAttribute> declareExchanges = GetAllAttributes<DeclareExchangeAttribute>(targetClass);
        ProcessDeclareExchanges(services, configuration, declareExchanges);

        List<DeclareQueueBindingAttribute> declareBindings = GetAllAttributes<DeclareQueueBindingAttribute>(targetClass);
        List<QueueBinding> bindings = ProcessDeclareQueueBindings(services, configuration, declareBindings);
        UpdateBindingDeclarations(bindings);
    }

    private static List<QueueBinding> ProcessDeclareQueueBindings(IServiceCollection services, IConfiguration configuration,
        List<DeclareQueueBindingAttribute> declareBindings)
    {
        var bindings = new List<QueueBinding>();

        foreach (DeclareQueueBindingAttribute b in declareBindings)
        {
            string bindingName = PropertyPlaceholderHelper.ResolvePlaceholders(b.Name, configuration);

            if (b.RoutingKeys.Length > 0)
            {
                foreach (string key in b.RoutingKeys)
                {
                    QueueBinding binding = BuildBinding(bindingName, b, configuration, key);
                    services.AddRabbitBinding(binding);
                    bindings.Add(binding);
                }
            }
            else
            {
                QueueBinding binding = BuildBinding(bindingName, b, configuration);
                services.AddRabbitBinding(binding);
                bindings.Add(binding);
            }
        }

        return bindings;
    }

    private static void ProcessDeclareExchanges(IServiceCollection services, IConfiguration configuration, List<DeclareExchangeAttribute> declareExchanges)
    {
        foreach (DeclareExchangeAttribute e in declareExchanges)
        {
            string exchangeName = PropertyPlaceholderHelper.ResolvePlaceholders(e.Name, configuration);
            IExchange exchange = CreateExchange(exchangeName, e.Type);

            if (!string.IsNullOrEmpty(e.Durable))
            {
                exchange.IsDurable = GetBoolean(e.Durable, configuration, nameof(e.Durable));
            }

            if (!string.IsNullOrEmpty(e.Delayed))
            {
                exchange.IsDelayed = GetBoolean(e.Delayed, configuration, nameof(e.Delayed));
            }

            if (!string.IsNullOrEmpty(e.AutoDelete))
            {
                exchange.IsAutoDelete = GetBoolean(e.AutoDelete, configuration, nameof(e.AutoDelete));
            }

            if (!string.IsNullOrEmpty(e.IgnoreDeclarationExceptions))
            {
                exchange.IgnoreDeclarationExceptions = GetBoolean(e.IgnoreDeclarationExceptions, configuration, nameof(e.IgnoreDeclarationExceptions));
            }

            if (!string.IsNullOrEmpty(e.Declare))
            {
                exchange.ShouldDeclare = GetBoolean(e.Declare, configuration, nameof(e.Declare));
            }

            if (!string.IsNullOrEmpty(e.Internal))
            {
                exchange.IsInternal = GetBoolean(e.Internal, configuration, nameof(e.Internal));
            }

            if (e.Admins.Length > 0)
            {
                foreach (string a in e.Admins)
                {
                    exchange.DeclaringAdmins.Add(a);
                }
            }

            services.AddRabbitExchange(exchange);
        }
    }

    private static void UpdateBindingDeclarations(List<QueueBinding> bindings)
    {
        foreach (QueueBinding binding in bindings)
        {
            BindingMap.TryAdd(binding.ServiceName, binding);
        }
    }

    private static void UpdateQueueDeclarations(List<Queue> queues)
    {
        foreach (Queue queue in queues)
        {
            QueueMap.TryAdd(queue.ServiceName, queue);
        }
    }

    private static QueueBinding BuildBinding(string bindingName, DeclareQueueBindingAttribute b, IConfiguration configuration, string routingKey = null)
    {
        var binding = new QueueBinding(bindingName);

        if (!string.IsNullOrEmpty(b.QueueName))
        {
            string reference = PropertyPlaceholderHelper.ResolvePlaceholders(b.QueueName, configuration);

            if (ConfigurationUtils.IsExpression(reference))
            {
                reference = ConfigurationUtils.ExtractExpressionString(reference);

                if (ConfigurationUtils.IsServiceReference(reference))
                {
                    reference = ConfigurationUtils.ExtractServiceName(reference);
                }
            }

            if (QueueMap.TryGetValue(reference, out Queue queueRef))
            {
                reference = queueRef.QueueName;
            }

            binding.Destination = reference;
        }

        binding.Exchange = !string.IsNullOrEmpty(b.ExchangeName) ? PropertyPlaceholderHelper.ResolvePlaceholders(b.ExchangeName, configuration) : string.Empty;

        if (!string.IsNullOrEmpty(b.IgnoreDeclarationExceptions))
        {
            binding.IgnoreDeclarationExceptions = GetBoolean(b.IgnoreDeclarationExceptions, configuration, nameof(b.IgnoreDeclarationExceptions));
        }

        if (!string.IsNullOrEmpty(b.Declare))
        {
            binding.ShouldDeclare = GetBoolean(b.Declare, configuration, nameof(b.Declare));
        }

        binding.RoutingKey = !string.IsNullOrEmpty(routingKey) ? PropertyPlaceholderHelper.ResolvePlaceholders(routingKey, configuration) : string.Empty;

        if (b.Admins.Length > 0)
        {
            foreach (string a in b.Admins)
            {
                binding.DeclaringAdmins.Add(a);
            }
        }

        return binding;
    }

    private static IExchange CreateExchange(string name, string type)
    {
        if (type == ExchangeType.Direct)
        {
            return new DirectExchange(name);
        }

        if (type == ExchangeType.FanOut)
        {
            return new FanOutExchange(name);
        }

        if (type == ExchangeType.Headers)
        {
            return new HeadersExchange(name);
        }

        if (type == ExchangeType.Topic)
        {
            return new TopicExchange(name);
        }

        if (type == ExchangeType.System)
        {
            return new CustomExchange(name, ExchangeType.System);
        }

        throw new InvalidOperationException($"Unable to determine exchange type {type}");
    }

    private static List<Queue> ProcessDeclareQueues(IServiceCollection services, IConfiguration configuration, List<DeclareQueueAttribute> declareQueues)
    {
        var queues = new List<Queue>();

        foreach (DeclareQueueAttribute q in declareQueues)
        {
            string queueName = PropertyPlaceholderHelper.ResolvePlaceholders(q.Name, configuration);
            var queue = new Queue(queueName);
            UpdateQueue(queue, q, configuration);
            services.AddRabbitQueue(queue);
            queues.Add(queue);
        }

        return queues;
    }

    private static List<Queue> ProcessDeclareAnonymousQueues(IServiceCollection services, IConfiguration configuration,
        List<DeclareAnonymousQueueAttribute> declareQueues)
    {
        var queues = new List<Queue>();

        foreach (DeclareAnonymousQueueAttribute q in declareQueues)
        {
            var queue = new Queue(q.Name, false, true, true)
            {
                ServiceName = q.Id
            };

            UpdateQueue(queue, q, configuration);
            services.AddRabbitQueue(queue);
            queues.Add(queue);
        }

        return queues;
    }

    private static void UpdateQueue(Queue queue, DeclareQueueBaseAttribute q, IConfiguration configuration)
    {
        if (!string.IsNullOrEmpty(q.Durable))
        {
            queue.IsDurable = GetBoolean(q.Durable, configuration, nameof(q.Durable));
        }

        if (!string.IsNullOrEmpty(q.Exclusive))
        {
            queue.IsExclusive = GetBoolean(q.Exclusive, configuration, nameof(q.Exclusive));
        }

        if (!string.IsNullOrEmpty(q.AutoDelete))
        {
            queue.IsAutoDelete = GetBoolean(q.AutoDelete, configuration, nameof(q.AutoDelete));
        }

        if (!string.IsNullOrEmpty(q.IgnoreDeclarationExceptions))
        {
            queue.IgnoreDeclarationExceptions = GetBoolean(q.IgnoreDeclarationExceptions, configuration, nameof(q.IgnoreDeclarationExceptions));
        }

        if (!string.IsNullOrEmpty(q.Declare))
        {
            queue.ShouldDeclare = GetBoolean(q.Declare, configuration, nameof(q.Declare));
        }

        if (q.Admins.Length > 0)
        {
            foreach (string a in q.Admins)
            {
                queue.DeclaringAdmins.Add(a);
            }
        }
    }

    private static bool GetBoolean(string value, IConfiguration configuration, string name)
    {
        value = PropertyPlaceholderHelper.ResolvePlaceholders(value, configuration);

        if (bool.TryParse(value, out bool result))
        {
            return result;
        }

        throw new InvalidOperationException($"Unable to parse annotation property: {name}");
    }

    private static List<T> GetAllAttributes<T>(Type targetClass)
        where T : Attribute
    {
        var results = new List<T>();

        IEnumerable<T> classLevel = targetClass.GetCustomAttributes<T>();
        results.AddRange(classLevel);
        MethodInfo[] reflectMethods = targetClass.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        foreach (MethodInfo m in reflectMethods)
        {
            IEnumerable<T> methodLevel = m.GetCustomAttributes<T>();
            results.AddRange(methodLevel);
        }

        return results;
    }
}
