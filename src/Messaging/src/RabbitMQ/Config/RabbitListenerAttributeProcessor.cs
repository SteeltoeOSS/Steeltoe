// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Configuration;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Converter;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Expression.Internal.Contexts;
using Steeltoe.Common.Order;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Messaging.Handler.Invocation;
using Steeltoe.Messaging.RabbitMQ.Attributes;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Listener.Adapters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Steeltoe.Messaging.RabbitMQ.Config
{
    public class RabbitListenerAttributeProcessor : IRabbitListenerAttributeProcessor, IOrdered
    {
        public const string DEFAULT_SERVICE_NAME = nameof(RabbitListenerAttributeProcessor);

        private readonly List<RabbitListenerMetadata> _rabbitListenerMetadata;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private int _counter;
        private IApplicationContext _applicationContext;

        public RabbitListenerAttributeProcessor(
            IApplicationContext applicationContext,
            IRabbitListenerEndpointRegistry endpointRegistry,
            IRabbitListenerEndpointRegistrar registrar,
            IMessageHandlerMethodFactory messageHandlerMethodFactory,
            IEnumerable<RabbitListenerMetadata> rabbitListeners,
            ILoggerFactory loggerFactory = null)
        {
            ApplicationContext = applicationContext;
            EndpointRegistry = endpointRegistry;
            registrar.EndpointRegistry = endpointRegistry;
            Registrar = registrar;
            MessageHandlerMethodFactory = messageHandlerMethodFactory;
            _rabbitListenerMetadata = rabbitListeners.ToList();
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory?.CreateLogger<RabbitListenerAttributeProcessor>();
        }

        public IApplicationContext ApplicationContext
        {
            get => _applicationContext;
            set
            {
                _applicationContext = value;
                if (_applicationContext != null)
                {
                    Resolver = _applicationContext.ServiceExpressionResolver ?? new StandardServiceExpressionResolver();
                    ExpressionContext = new ServiceExpressionContext(_applicationContext);
                }
            }
        }

        public int Order { get; } = AbstractOrdered.LOWEST_PRECEDENCE;

        public IServiceExpressionResolver Resolver { get; set; } = new StandardServiceExpressionResolver();

        public IServiceExpressionContext ExpressionContext { get; set; }

        public IServiceResolver ServiceResolver { get; set; }

        public IRabbitListenerEndpointRegistrar Registrar { get; }

        public IRabbitListenerEndpointRegistry EndpointRegistry { get; }

        public Encoding Charset { get; set; } = EncodingUtils.Utf8;

        public string ContainerFactoryServiceName { get; set; } = DirectRabbitListenerContainerFactory.DEFAULT_SERVICE_NAME;

        public string ServiceName { get; set; } = DEFAULT_SERVICE_NAME;

        internal IMessageHandlerMethodFactory MessageHandlerMethodFactory { get; set; }

        public void Initialize()
        {
            _logger?.LogDebug("RabbitListenerAttributeProcessor initializing");

            foreach (var metadata in _rabbitListenerMetadata)
            {
                var bean = CreateTargetBean(metadata.TargetClass);
                var beanName = metadata.TargetClass.Name;
                foreach (var lm in metadata.ListenerMethods)
                {
                    foreach (var rabbitListener in lm.Attributes)
                    {
                        ProcessAmqpListener(rabbitListener, lm.Method, bean, beanName);
                    }
                }

                if (metadata.HandlerMethods.Count > 0)
                {
                    ProcessMultiMethodListeners(metadata.ClassAnnotations, metadata.HandlerMethods, bean, beanName);
                }
            }

            Registrar.ApplicationContext = ApplicationContext;
            var instances = ApplicationContext.GetServices<IRabbitListenerConfigurer>();
            foreach (var configurer in instances)
            {
                configurer.ConfigureRabbitListeners(Registrar);
            }

            if (ContainerFactoryServiceName != null)
            {
                Registrar.ContainerFactoryServiceName = ContainerFactoryServiceName;
            }

            var handlerMethodFactory = Registrar.MessageHandlerMethodFactory;
            if (handlerMethodFactory != null)
            {
                MessageHandlerMethodFactory = handlerMethodFactory;
            }

            _logger?.LogDebug("Initializing IRabbitListenerEndpointRegistrar");
            Registrar.Initialize();
        }

        protected void ProcessMultiMethodListeners(List<RabbitListenerAttribute> classLevelListeners, List<MethodInfo> multiMethods, object bean, string beanName)
        {
            var checkedMethods = new List<MethodInfo>();
            MethodInfo defaultMethod = null;
            foreach (var method in multiMethods)
            {
                var attribute = method.GetCustomAttribute<RabbitHandlerAttribute>();
                if (attribute == null)
                {
                    throw new InvalidOperationException("Multimethod must contain RabbitHandlerAttribute");
                }

                if (attribute.IsDefault)
                {
                    var toAssert = defaultMethod;
                    if (toAssert != null)
                    {
                        throw new InvalidOperationException("Only one RabbitHandlerAttribute can be marked 'isDefault', found: " + toAssert.ToString() + " and " + method.ToString());
                    }

                    defaultMethod = method;
                }

                _logger?.LogDebug("Adding RabbitHandler method {handlerMethod} from type {type}", method.ToString(), method.DeclaringType);
                checkedMethods.Add(method);
            }

            foreach (var classLevelListener in classLevelListeners)
            {
                var endpoint = new MultiMethodRabbitListenerEndpoint(ApplicationContext, checkedMethods, defaultMethod, bean, _loggerFactory);
                ProcessListener(endpoint, classLevelListener, bean, bean.GetType(), beanName);
            }
        }

        protected void ProcessListener(MethodRabbitListenerEndpoint endpoint, RabbitListenerAttribute rabbitListener, object bean, object target, string beanName)
        {
            endpoint.MessageHandlerMethodFactory = MessageHandlerMethodFactory;
            endpoint.Id = GetEndpointId(rabbitListener);
            endpoint.SetQueueNames(ResolveQueues(rabbitListener));
            endpoint.Concurrency = ResolveExpressionAsInteger(rabbitListener.Concurrency, "Concurrency");
            endpoint.ApplicationContext = ApplicationContext;
            endpoint.ReturnExceptions = ResolveExpressionAsBoolean(rabbitListener.ReturnExceptions, "ReturnExceptions");

            var group = rabbitListener.Group;
            if (!string.IsNullOrEmpty(group))
            {
                endpoint.Group = group;
            }

            var autoStartup = rabbitListener.AutoStartup;
            if (!string.IsNullOrEmpty(autoStartup))
            {
                endpoint.AutoStartup = ResolveExpressionAsBoolean(autoStartup, "AutoStartup");
            }

            endpoint.Exclusive = rabbitListener.Exclusive;

            endpoint.Priority = ResolveExpressionAsInteger(rabbitListener.Priority, "Priority");

            ResolveErrorHandler(endpoint, rabbitListener);
            ResolveAdmin(endpoint, rabbitListener, target);
            ResolveAckMode(endpoint, rabbitListener);
            ResolvePostProcessor(endpoint, rabbitListener, target, beanName);
            var factory = ResolveContainerFactory(rabbitListener, target, beanName);

            Registrar.RegisterEndpoint(endpoint, factory);
        }

        private void ProcessAmqpListener(RabbitListenerAttribute rabbitListener, MethodInfo method, object bean, string beanName)
        {
            _logger?.LogDebug("Adding RabbitListener method {method} from type {type}", method.ToString(), method.DeclaringType);
            var endpoint = new MethodRabbitListenerEndpoint(ApplicationContext, method, bean, _loggerFactory);
            endpoint.Method = method;
            ProcessListener(endpoint, rabbitListener, bean, method, beanName);
        }

        private void ResolvePostProcessor(MethodRabbitListenerEndpoint endpoint, RabbitListenerAttribute rabbitListener, object target, string name)
        {
            var ppBeanName = Resolve(rabbitListener.ReplyPostProcessor);
            if (!string.IsNullOrEmpty(ppBeanName))
            {
                if (ApplicationContext == null)
                {
                    throw new InvalidOperationException("IApplicationContext must be set to resolve reply post processor by name");
                }

                var pp = ApplicationContext.GetService<IReplyPostProcessor>(ppBeanName);
                if (pp == null)
                {
                    throw new InvalidOperationException("Could not register rabbit listener endpoint on [" +
                            target + "], no IReplyPostProcessor with id '" + name + "' was found");
                }

                endpoint.ReplyPostProcessor = pp;
            }
        }

        private IRabbitListenerContainerFactory ResolveContainerFactory(RabbitListenerAttribute rabbitListener, object factoryTarget, string name)
        {
            IRabbitListenerContainerFactory factory = null;
            var containerFactoryBeanName = Resolve(rabbitListener.ContainerFactory);
            if (!string.IsNullOrEmpty(containerFactoryBeanName))
            {
                if (ApplicationContext == null)
                {
                    throw new InvalidOperationException("IApplicationContext must be set to resolve container factory by name");
                }

                factory = ApplicationContext.GetService<IRabbitListenerContainerFactory>(containerFactoryBeanName);
                if (factory == null)
                {
                    throw new InvalidOperationException("Could not register rabbit listener endpoint on [" +
                            factoryTarget + "], no IRabbitListenerContainerFactory with id '" + containerFactoryBeanName + "' was found");
                }
            }

            return factory;
        }

        private void ResolveAdmin(MethodRabbitListenerEndpoint endpoint, RabbitListenerAttribute rabbitListener, object adminTarget)
        {
            var rabbitAdmin = Resolve(rabbitListener.Admin);
            if (!string.IsNullOrEmpty(rabbitAdmin))
            {
                if (ApplicationContext == null)
                {
                    throw new InvalidOperationException("IApplicationContext must be set to resolve RabbitAdmin by name");
                }

                endpoint.Admin = ApplicationContext.GetService<IRabbitAdmin>(rabbitAdmin);
                if (endpoint.Admin == null)
                {
                    throw new InvalidOperationException("Could not register rabbit listener endpoint on [" +
                            adminTarget + "], no RabbitAdmin with id '" + rabbitAdmin + "' was found");
                }
            }
        }

        private void ResolveErrorHandler(MethodRabbitListenerEndpoint endpoint, RabbitListenerAttribute rabbitListener)
        {
            if (!string.IsNullOrEmpty(rabbitListener.ErrorHandler))
            {
                var errorHandler = ResolveExpression(rabbitListener.ErrorHandler, typeof(IRabbitListenerErrorHandler), "ErrorHandler");
                if (errorHandler is IRabbitListenerErrorHandler)
                {
                    endpoint.ErrorHandler = (IRabbitListenerErrorHandler)errorHandler;
                }
                else if (errorHandler is string errorHandlerName)
                {
                    if (ApplicationContext == null)
                    {
                        throw new InvalidOperationException("IApplicationContext must be set to resolve ErrorHandler by name");
                    }

                    endpoint.ErrorHandler = ApplicationContext.GetService<IRabbitListenerErrorHandler>(errorHandlerName);
                    if (endpoint.ErrorHandler == null)
                    {
                        throw new InvalidOperationException("Failed to resolve ErrorHandler by name using: " + errorHandlerName);
                    }
                }
                else
                {
                    throw new InvalidOperationException("ErrorHandler must resolve to a String or IRabbitListenerErrorHandler");
                }
            }
        }

        private void ResolveAckMode(MethodRabbitListenerEndpoint endpoint, RabbitListenerAttribute rabbitListener)
        {
            if (!string.IsNullOrEmpty(rabbitListener.AckMode))
            {
                var ackMode = ResolveExpression(rabbitListener.AckMode, typeof(AcknowledgeMode), "AckMode");
                if (ackMode is AcknowledgeMode mode)
                {
                    endpoint.AckMode = mode;
                }
                else if (ackMode is string ackModeString)
                {
                    endpoint.AckMode = (AcknowledgeMode)Enum.Parse(typeof(AcknowledgeMode), ackModeString);
                }
                else
                {
                    throw new InvalidOperationException("AckMode must resolve to a String or AcknowledgeMode enumeration");
                }
            }
        }

        private bool ResolveExpressionAsBoolean(string value, string propertyName, bool defaultValue = false)
        {
            if (!string.IsNullOrEmpty(value))
            {
                var resolved = ResolveExpression(value, typeof(bool), propertyName);
                if (resolved is bool)
                {
                    return (bool)resolved;
                }
                else if (resolved is string resolvedString)
                {
                    if (bool.TryParse((string)resolvedString, out var result))
                    {
                        return result;
                    }

                    throw new InvalidOperationException("Unable to resolve " + propertyName + " to a bool using " + resolvedString);
                }
            }

            return defaultValue;
        }

        private int? ResolveExpressionAsInteger(string value, string propertyName)
        {
            if (!string.IsNullOrEmpty(value))
            {
                var resolved = ResolveExpression(value, typeof(int), propertyName);
                if (resolved is int)
                {
                    return (int)resolved;
                }
                else if (resolved is string resolvedString && int.TryParse((string)resolvedString, out var result))
                {
                    return result;
                }

                throw new InvalidOperationException("Unable to resolve " + propertyName + " to an int using " + resolved);
            }

            return null;
        }

        private string[] ResolveQueues(RabbitListenerAttribute rabbitListener)
        {
            // var allQueues = ApplicationContext.GetServices<IQueue>();
            var queues = rabbitListener.Queues;

            var result = new List<string>();
            if (queues.Length > 0)
            {
                for (var i = 0; i < queues.Length; i++)
                {
                    var queueExpression = queues[i];
                    ResolveQueue(queueExpression, result);
                }
            }

            // var allBindings = ApplicationContext.GetServices<IBinding>();
            var bindings = rabbitListener.Bindings;
            if (bindings.Length > 0)
            {
                ResolveBindingDeclaration(rabbitListener, result);
            }

            return result.ToArray();
        }

        private void ResolveQueue(string queueExpression, List<string> results)
        {
            string qRef = queueExpression;
            var queue = ResolveExpression(queueExpression, typeof(IQueue), "Queue(s)") as IQueue;
            if (queue == null)
            {
                qRef = Resolve(queueExpression);
                queue = ApplicationContext.GetService<IQueue>(qRef);
            }

            if (queue != null)
            {
                qRef = queue.QueueName;
            }

            results.Add(qRef);
        }

        private void ResolveBindingDeclaration(RabbitListenerAttribute rabbitListener, List<string> results)
        {
            foreach (var bindingExpression in rabbitListener.Bindings)
            {
                var binding = ResolveExpression(bindingExpression, typeof(IBinding), "Binding(s)") as IBinding;
                if (binding == null)
                {
                    var bindingName = Resolve(bindingExpression);
                    binding = ApplicationContext.GetService<IBinding>(bindingName);
                    if (binding == null)
                    {
                        throw new InvalidOperationException("Unable to resolve binding: " + bindingExpression + " using: " + bindingName);
                    }
                }

                ResolveQueue(binding.Destination, results);
            }
        }

        private void ResolveAsString(object resolvedValue, List<string> result, bool canBeQueue, string what)
        {
            var resolvedValueToUse = resolvedValue;
            if (resolvedValue is string[])
            {
                resolvedValueToUse = new List<string>((string[])resolvedValue);
            }

            if (canBeQueue && resolvedValueToUse is Config.IQueue)
            {
                result.Add(((Config.IQueue)resolvedValueToUse).QueueName);
            }
            else if (resolvedValueToUse is string)
            {
                var asString = resolvedValueToUse as string;
                result.Add(asString);
            }
            else if (resolvedValueToUse is IEnumerable)
            {
                foreach (var o in (IEnumerable)resolvedValueToUse)
                {
                    ResolveAsString(o, result, canBeQueue, what);
                }
            }
            else
            {
                throw new InvalidOperationException(
                    string.Format("RabbitListenerAttribute " + what + " can't resolve {0} as a String[] or a String " + (canBeQueue ? "or a Queue" : string.Empty), resolvedValue));
            }
        }

        private object ResolveExpression(string value, Type expectedType, string propertyName)
        {
            var resolvedValue = Resolve(value);
            return Resolver.Evaluate(resolvedValue, ExpressionContext);
        }

        private string Resolve(string value)
        {
            string result = value;
            if (ApplicationContext != null)
            {
                var config = ApplicationContext.Configuration;
                if (config != null)
                {
                    result = PropertyPlaceholderHelper.ResolvePlaceholders(value, config, _logger);
                }
            }

            return result;
        }

        private string GetEndpointId(RabbitListenerAttribute rabbitListener)
        {
            if (!string.IsNullOrEmpty(rabbitListener.Id))
            {
                return Resolve(rabbitListener.Id);
            }
            else
            {
                return "Steeltoe.Messaging.Rabbit.RabbitListenerEndpointContainer#" + Interlocked.Increment(ref _counter);
            }
        }

        private object CreateTargetBean(Type implementation)
        {
            try
            {
                _logger?.LogDebug("Creating RabbitListener service {serviceType}.", implementation.ToString());
                return ApplicationContext.ServiceProvider.GetService(implementation);
            }
            catch (Exception e)
            {
                // Log
                _logger?.LogError(e, "Error creating RabbitListener service {serviceType}.", implementation);
                throw new InvalidOperationException("Unable to CreateInstance of type containing RabbitListener method, Type: " + implementation, e);
            }
        }
    }
}
