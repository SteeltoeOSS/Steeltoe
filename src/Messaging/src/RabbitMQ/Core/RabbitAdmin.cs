// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Exceptions;
using Steeltoe.Common;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Retry;
using Steeltoe.Common.RetryPolly;
using Steeltoe.Messaging.RabbitMQ.Configuration;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using static Steeltoe.Messaging.RabbitMQ.Connection.CachingConnectionFactory;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Core;

public class RabbitAdmin : IRabbitAdmin, IConnectionListener
{
    private const int DeclareMaxAttempts = 5;
    private const int DeclareInitialRetryInterval = 1000;
    private const int DeclareMaxRetryInterval = 5000;
    private const double DeclareRetryMultiplier = 2.0d;
    private const string DelayedMessageExchange = "x-delayed-message";
    public const string DefaultServiceName = "rabbitAdmin";

    public const string QueueName = "QUEUE_NAME";
    public const string QueueMessageCount = "QUEUE_MESSAGE_COUNT";
    public const string QueueConsumerCount = "QUEUE_CONSUMER_COUNT";

    private readonly object _lifecycleMonitor = new();
    private readonly ILogger _logger;
    private int _initializing;

    public IApplicationContext ApplicationContext { get; set; }

    public string ServiceName { get; set; } = DefaultServiceName;

    public IConnectionFactory ConnectionFactory { get; set; }

    public RabbitTemplate RabbitTemplate { get; }

    public bool AutoStartup { get; set; }

    public bool IgnoreDeclarationExceptions { get; set; }

    public DeclarationExceptionEvent LastDeclarationExceptionEvent { get; private set; }

    public bool ExplicitDeclarationsOnly { get; set; }

    public bool IsAutoStartup { get; set; } = true;

    public bool IsRunning { get; private set; }

    public RetryTemplate RetryTemplate { get; set; }

    public bool RetryDisabled { get; set; }

    [ActivatorUtilitiesConstructor]
    public RabbitAdmin(IApplicationContext applicationContext, IConnectionFactory connectionFactory, ILogger logger = null)
    {
        ArgumentGuard.NotNull(connectionFactory);

        _logger = logger;
        ApplicationContext = applicationContext;
        ConnectionFactory = connectionFactory;
        RabbitTemplate = new RabbitTemplate(connectionFactory, logger);
        DoInitialize();
    }

    public RabbitAdmin(IConnectionFactory connectionFactory, ILogger logger = null)
        : this(null, connectionFactory, logger)
    {
    }

    public RabbitAdmin(RabbitTemplate template, ILogger logger = null)
    {
        ArgumentGuard.NotNull(template);

        _logger = logger;
        RabbitTemplate = template;

        ConnectionFactory = template.ConnectionFactory ??
            throw new ArgumentException($"{nameof(template.ConnectionFactory)} in {nameof(template)} must not be null.", nameof(template));

        DoInitialize();
    }

    public void DeclareBinding(IBinding binding)
    {
        try
        {
            RabbitTemplate.Execute<object>(channel =>
            {
                DeclareBindings(channel, binding);
                return null;
            });
        }
        catch (RabbitException e)
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
        catch (RabbitException e)
        {
            LogOrRethrowDeclarationException(exchange, "exchange", e);
        }
    }

    public IQueue DeclareQueue()
    {
        try
        {
            RC.QueueDeclareOk declareOk = RabbitTemplate.Execute(channel => RC.IModelExensions.QueueDeclare(channel));
            return new Queue(declareOk.QueueName, false, true, true);
        }
        catch (RabbitException e)
        {
            LogOrRethrowDeclarationException(null, "queue", e);
            return null;
        }
    }

    public string DeclareQueue(IQueue queue)
    {
        try
        {
            return RabbitTemplate.Execute(channel =>
            {
                List<RC.QueueDeclareOk> declared = DeclareQueues(channel, queue);
                return declared.Count > 0 ? declared[0].QueueName : null;
            });
        }
        catch (RabbitException e)
        {
            LogOrRethrowDeclarationException(queue, "queue", e);
            return null;
        }
    }

    public bool DeleteExchange(string exchangeName)
    {
        return RabbitTemplate.Execute(channel =>
        {
            if (IsDeletingDefaultExchange(exchangeName))
            {
                return true;
            }

            try
            {
                channel.ExchangeDelete(exchangeName, false);
            }
            catch (RabbitMQClientException e)
            {
                _logger?.LogError(e, "Exception while issuing ExchangeDelete");
                return false;
            }

            return true;
        });
    }

    public bool DeleteQueue(string queueName)
    {
        return RabbitTemplate.Execute(channel =>
        {
            try
            {
                RC.IModelExensions.QueueDelete(channel, queueName);
                return true;
            }
            catch (RabbitMQClientException e)
            {
                _logger?.LogError(e, "Exception while issuing QueueDelete");
                return false;
            }
        });
    }

    public void DeleteQueue(string queueName, bool unused, bool empty)
    {
        RabbitTemplate.Execute<object>(channel =>
        {
            channel.QueueDelete(queueName, unused, empty);
            return null;
        });
    }

    public QueueInformation GetQueueInfo(string queueName)
    {
        ArgumentGuard.NotNullOrEmpty(queueName);

        if (queueName.Length > 255)
        {
            return null;
        }

        return RabbitTemplate.Execute(channel =>
        {
            try
            {
                RC.QueueDeclareOk declareOk = channel.QueueDeclarePassive(queueName);
                return new QueueInformation(declareOk.QueueName, declareOk.MessageCount, declareOk.ConsumerCount);
            }
            catch (RabbitMQClientException e)
            {
                _logger?.LogError(e, "Exception while fetching Queue properties for '{queueName}'", queueName);

                try
                {
                    if (channel is IChannelProxy proxy)
                    {
                        proxy.TargetChannel.Close();
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Exception while closing {channel}", channel);
                }

                return null;
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Queue '{queueName}' does not exist", queueName);
                return null;
            }
        });
    }

    public Dictionary<string, object> GetQueueProperties(string queueName)
    {
        QueueInformation queueInfo = GetQueueInfo(queueName);

        if (queueInfo != null)
        {
            var props = new Dictionary<string, object>
            {
                { QueueName, queueInfo.Name },
                { QueueMessageCount, queueInfo.MessageCount },
                { QueueConsumerCount, queueInfo.ConsumerCount }
            };

            return props;
        }

        return null;
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
        return RabbitTemplate.Execute(channel =>
        {
            uint queuePurged = channel.QueuePurge(queueName);
            _logger?.LogDebug("Purged queue: {queueName} : {result}", queueName, queuePurged);
            return queuePurged;
        });
    }

    public void RemoveBinding(IBinding binding)
    {
        RabbitTemplate.Execute<object>(channel =>
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

    public void OnCreate(IConnection connection)
    {
        _logger?.LogDebug("OnCreate for connection: {connection}", connection);

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
                RetryTemplate.Execute(c =>
                {
                    _logger?.LogTrace($"Rabbit Admin::Initialize(). Context: {c}");
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

    public void OnClose(IConnection connection)
    {
        _logger?.LogDebug("OnClose for connection: {connection}", connection);
    }

    public void OnShutDown(RC.ShutdownEventArgs args)
    {
        _logger?.LogDebug("OnShutDown for connection: {args}", args);
    }

    public void Initialize()
    {
        _logger?.LogDebug("Initializing declarations");

        if (ApplicationContext == null)
        {
            _logger?.LogDebug("No ApplicationContext has been set, cannot auto-declare Exchanges, Queues, and Bindings");
            return;
        }

        List<IExchange> contextExchanges = ApplicationContext.GetServices<IExchange>().ToList();
        List<IQueue> contextQueues = ApplicationContext.GetServices<IQueue>().ToList();
        List<IBinding> contextBindings = ApplicationContext.GetServices<IBinding>().ToList();
        List<IDeclarableCustomizer> customizers = ApplicationContext.GetServices<IDeclarableCustomizer>().ToList();

        ProcessDeclarables(contextExchanges, contextQueues, contextBindings);

        List<IExchange> exchanges = FilterDeclarables(contextExchanges, customizers);
        List<IQueue> queues = FilterDeclarables(contextQueues, customizers);
        List<IBinding> bindings = FilterDeclarables(contextBindings, customizers);

        foreach (IExchange exchange in exchanges)
        {
            if (!exchange.IsDurable || exchange.IsAutoDelete)
            {
                _logger?.LogInformation(
                    "Auto-declaring a non-durable or auto-delete Exchange ({exchange}), durable:{durable}, auto-delete:{autoDelete}. " +
                    "It will be deleted by the broker if it shuts down, and can be redeclared by closing and reopening the connection.", exchange.ExchangeName,
                    exchange.IsDurable, exchange.IsAutoDelete);
            }
        }

        foreach (IQueue queue in queues)
        {
            if (!queue.IsDurable || queue.IsAutoDelete || queue.IsExclusive)
            {
                _logger?.LogInformation(
                    "Auto-declaring a non-durable, auto-delete, or exclusive Queue ({queueName}) durable:{durable}, auto-delete:{autoDelete}, exclusive:{exclusive}." +
                    "It will be redeclared if the broker stops and is restarted while the connection factory is alive, but all messages will be lost.",
                    queue.QueueName, queue.IsDurable, queue.IsAutoDelete, queue.IsExclusive);
            }
        }

        if (exchanges.Count == 0 && queues.Count == 0 && bindings.Count == 0)
        {
            _logger?.LogDebug("Nothing to declare");
            return;
        }

        RabbitTemplate.Execute<object>(channel =>
        {
            DeclareExchanges(channel, exchanges.ToArray());
            DeclareQueues(channel, queues.ToArray());
            DeclareBindings(channel, bindings.ToArray());
            return null;
        });

        _logger?.LogDebug("Declarations finished");
    }

    private void ProcessDeclarables(List<IExchange> contextExchanges, List<IQueue> contextQueues, List<IBinding> contextBindings)
    {
        IEnumerable<Declarables> declarables = ApplicationContext.GetServices<Declarables>();

        foreach (Declarables declarable in declarables)
        {
            foreach (IDeclarable d in declarable.DeclarableList)
            {
                switch (d)
                {
                    case IExchange exD:
                        contextExchanges.Add(exD);
                        break;
                    case IQueue qD:
                        contextQueues.Add(qD);
                        break;
                    case IBinding bD:
                        contextBindings.Add(bD);
                        break;
                }
            }
        }
    }

    private List<T> FilterDeclarables<T>(IEnumerable<T> declarables, IEnumerable<IDeclarableCustomizer> customizers)
        where T : IDeclarable
    {
        var results = new List<T>();
        int customizerCounts = customizers.Count();

        foreach (T dec in declarables)
        {
            if (ShouldDeclare(dec))
            {
                if (customizerCounts == 0)
                {
                    results.Add(dec);
                }
                else
                {
                    IDeclarable customized = dec;

                    foreach (IDeclarableCustomizer customizer in customizers)
                    {
                        customized = customizer.Apply(customized);
                    }

                    if (customized != null)
                    {
                        results.Add((T)customized);
                    }
                }
            }
        }

        return results;
    }

    private bool ShouldDeclare<T>(T declarable)
        where T : IDeclarable
    {
        if (!declarable.ShouldDeclare)
        {
            return false;
        }

        if (declarable.DeclaringAdmins.Count == 0 && !ExplicitDeclarationsOnly)
        {
            return true;
        }

        if (declarable.DeclaringAdmins.Contains(this))
        {
            return true;
        }

        return ServiceName != null && declarable.DeclaringAdmins.Contains(ServiceName);
    }

    private void DeclareExchanges(RC.IModel channel, params IExchange[] exchanges)
    {
        foreach (IExchange exchange in exchanges)
        {
            _logger?.LogDebug("Declaring exchange '{exchange}'", exchange.ExchangeName);

            if (!IsDeclaringDefaultExchange(exchange))
            {
                try
                {
                    if (exchange.IsDelayed)
                    {
                        Dictionary<string, object> arguments = exchange.Arguments;
                        arguments = arguments == null ? new Dictionary<string, object>() : new Dictionary<string, object>(arguments);

                        arguments["x-delayed-type"] = exchange.Type;

                        channel.ExchangeDeclare(exchange.ExchangeName, DelayedMessageExchange, exchange.IsDurable, exchange.IsAutoDelete, arguments);
                    }
                    else
                    {
                        channel.ExchangeDeclare(exchange.ExchangeName, exchange.Type, exchange.IsDurable, exchange.IsAutoDelete, exchange.Arguments);
                    }
                }
                catch (Exception e)
                {
                    LogOrRethrowDeclarationException(exchange, "exchange", e);
                }
            }
        }
    }

    private List<RC.QueueDeclareOk> DeclareQueues(RC.IModel channel, params IQueue[] queues)
    {
        var declareOks = new List<RC.QueueDeclareOk>(queues.Length);

        foreach (IQueue queue in queues)
        {
            if (!queue.QueueName.StartsWith("amq.", StringComparison.Ordinal))
            {
                _logger?.LogDebug("Declaring Queue '{queueName}'", queue.QueueName);

                if (queue.QueueName.Length > 255)
                {
                    throw new ArgumentException("Queue names cannot be longer than 255 characters.", nameof(queues));
                }

                try
                {
                    try
                    {
                        RC.QueueDeclareOk declareOk =
                            channel.QueueDeclare(queue.QueueName, queue.IsDurable, queue.IsExclusive, queue.IsAutoDelete, queue.Arguments);

                        if (!string.IsNullOrEmpty(declareOk.QueueName))
                        {
                            queue.ActualName = declareOk.QueueName;
                        }

                        declareOks.Add(declareOk);
                    }
                    catch (ArgumentException)
                    {
                        CloseChannelAfterIllegalArg(channel, queue);
                        throw;
                    }
                }
                catch (Exception e)
                {
                    LogOrRethrowDeclarationException(queue, "queue", e);
                }
            }
            else
            {
                _logger?.LogDebug("Queue with name: {queueName} that starts with 'amq.' cannot be declared.", queue.QueueName);
            }
        }

        return declareOks;
    }

    private void CloseChannelAfterIllegalArg(RC.IModel channel, IQueue queue)
    {
        _logger?.LogError("Exception while declaring queue'{queueName}'", queue.QueueName);

        try
        {
            if (channel is IChannelProxy proxy)
            {
                proxy.TargetChannel.Close();
            }
        }
        catch (Exception exception)
        {
            _logger?.LogError(exception, "Failed to close {channel} after illegal argument", channel);
        }
    }

    private void DeclareBindings(RC.IModel channel, params IBinding[] bindings)
    {
        foreach (IBinding binding in bindings)
        {
            _logger?.LogDebug("Binding destination [{destination} ({type})] to exchange [{exchange}] with routing key [{routingKey}]", binding.Destination,
                binding.Type, binding.Exchange, binding.RoutingKey);

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
            catch (Exception e)
            {
                LogOrRethrowDeclarationException(binding, "binding", e);
            }
        }
    }

    private void LogOrRethrowDeclarationException(IDeclarable element, string elementType, Exception exception)
    {
        PublishDeclarationExceptionEvent(element, exception);

        if (IgnoreDeclarationExceptions || (element != null && element.IgnoreDeclarationExceptions))
        {
            string elementText = element == null ? "broker-generated" : element.ToString();

            _logger?.LogDebug(exception, "Failed to declare {elementType}: {elementText}, continuing...", elementType, elementText);

            Exception cause = exception.InnerException ?? exception;

            _logger?.LogWarning(exception, "Failed to declare {elementType}: {element}, continuing... {cause}", elementType, elementText, cause);
        }
        else
        {
            throw exception;
        }
    }

    private void PublishDeclarationExceptionEvent(IDeclarable element, Exception exception)
    {
        var ev = new DeclarationExceptionEvent(this, element, exception);
        LastDeclarationExceptionEvent = ev;
    }

    private bool IsDeclaringDefaultExchange(IExchange exchange)
    {
        if (IsDefaultExchange(exchange.ExchangeName))
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

    private bool IsDeclaringImplicitQueueBinding(IBinding binding)
    {
        if (IsImplicitQueueBinding(binding))
        {
            _logger?.LogDebug("The default exchange is implicitly bound to every queue, with a routing key equal to the queue name.");
            return true;
        }

        return false;
    }

    private bool IsRemovingImplicitQueueBinding(IBinding binding)
    {
        if (IsImplicitQueueBinding(binding))
        {
            _logger?.LogDebug("Cannot remove implicit default exchange binding to queue.");
            return true;
        }

        return false;
    }

    private bool IsImplicitQueueBinding(IBinding binding)
    {
        return IsDefaultExchange(binding.Exchange) && binding.Destination == binding.RoutingKey;
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
                RetryTemplate = new PollyRetryTemplate(new Dictionary<Type, bool>(), DeclareMaxAttempts, true, DeclareInitialRetryInterval,
                    DeclareMaxRetryInterval, DeclareRetryMultiplier, _logger);
            }

            if (ConnectionFactory is CachingConnectionFactory factory && factory.CacheMode == CachingMode.Connection)
            {
                _logger?.LogWarning("RabbitAdmin auto declaration is not supported with CacheMode.CONNECTION");
                return;
            }

            ConnectionFactory.AddConnectionListener(this);
            IsRunning = true;
        }
    }
}
