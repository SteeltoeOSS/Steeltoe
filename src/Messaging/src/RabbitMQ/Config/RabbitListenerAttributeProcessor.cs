// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Configuration;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Converter;
using Steeltoe.Common.Order;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Messaging.Handler.Invocation;
using Steeltoe.Messaging.Rabbit.Attributes;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Expressions;
using Steeltoe.Messaging.Rabbit.Listener;
using Steeltoe.Messaging.Rabbit.Listener.Adapters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Steeltoe.Messaging.Rabbit.Config
{
    public class RabbitListenerAttributeProcessor
    {
        private readonly List<RabbitListenerMetadata> _rabbitListenerMetadata;
        private readonly ILogger _logger;
        private int _counter;

        public RabbitListenerAttributeProcessor(
            IApplicationContext applicationContext,
            RabbitListenerEndpointRegistry endpointRegistry,
            IEnumerable<RabbitListenerMetadata> rabbitListeners,
            ILogger logger = null)
        {
            ApplicationContext = applicationContext;
            EndpointRegistry = endpointRegistry;
            Registrar = new RabbitListenerEndpointRegistrar()
            {
                EndpointRegistry = endpointRegistry
            };
            MessageHandlerMethodFactory = new RabbitHandlerMethodFactoryAdapter(this);
            _rabbitListenerMetadata = rabbitListeners.ToList();
            _logger = logger;
        }

        public IApplicationContext ApplicationContext { get; set; }

        public int Order { get; } = AbstractOrdered.LOWEST_PRECEDENCE;

        public IServiceExpressionResolver Resolver { get; set; } // = new StandardBeanExpressionResolver();

        public IServiceExpressionContext ExpressionContext { get; set; }

        public IServiceResolver ServiceResolver { get; set; }

        public RabbitListenerEndpointRegistrar Registrar { get; }

        public RabbitListenerEndpointRegistry EndpointRegistry { get; }

        public Encoding Charset { get; set; }

        public string ContainerFactoryServiceName { get; set; } = IRabbitListenerContainerFactory.DEFAULT_RABBIT_LISTENER_CONTAINER_FACTORY_SERVICE_NAME;

        private RabbitHandlerMethodFactoryAdapter MessageHandlerMethodFactory { get; }

        public void Initialize()
        {
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
                MessageHandlerMethodFactory.Factory = handlerMethodFactory;
            }

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

                checkedMethods.Add(method);
            }

            foreach (var classLevelListener in classLevelListeners)
            {
                var endpoint = new MultiMethodRabbitListenerEndpoint(ApplicationContext, checkedMethods, defaultMethod, bean);
                ProcessListener(endpoint, classLevelListener, bean, bean.GetType(), beanName);
            }
        }

        protected void ProcessListener(MethodRabbitListenerEndpoint endpoint, RabbitListenerAttribute rabbitListener, object bean, object target, string beanName)
        {
            endpoint.MessageHandlerMethodFactory = MessageHandlerMethodFactory;
            endpoint.Id = GetEndpointId(rabbitListener);
            endpoint.SetQueueNames(ResolveQueues(rabbitListener));
            endpoint.Concurrency = ResolveExpressionAsInteger(rabbitListener.Concurrency, "concurrency");
            endpoint.ApplicationContext = ApplicationContext;
            endpoint.ReturnExceptions = ResolveExpressionAsBoolean(rabbitListener.ReturnExceptions);
            var errorHandler = ResolveExpression(rabbitListener.ErrorHandler);
            if (errorHandler is IRabbitListenerErrorHandler)
            {
                endpoint.ErrorHandler = (IRabbitListenerErrorHandler)errorHandler;
            }
            else if (errorHandler is string errorHandlerName)
            {
                if (!string.IsNullOrEmpty(errorHandlerName))
                {
                    endpoint.ErrorHandler = ApplicationContext.GetService<IRabbitListenerErrorHandler>(errorHandlerName);
                }
            }
            else
            {
                throw new InvalidOperationException("Error handler must be a service name or IRabbitListenerErrorHandler, not a " + errorHandler.GetType().ToString());
            }

            var autoStartup = rabbitListener.AutoStartup;
            if (!string.IsNullOrEmpty(autoStartup))
            {
                endpoint.AutoStartup = ResolveExpressionAsBoolean(autoStartup);
            }

            endpoint.Exclusive = rabbitListener.Exclusive;

            endpoint.Priority = ResolveExpressionAsInteger(rabbitListener.Priority, "priority");

            ResolveAdmin(endpoint, rabbitListener, target);
            ResolveAckMode(endpoint, rabbitListener);
            ResolvePostProcessor(endpoint, rabbitListener, target, beanName);
            var factory = ResolveContainerFactory(rabbitListener, target, beanName);

            Registrar.RegisterEndpoint(endpoint, factory);
        }

        private void ProcessAmqpListener(RabbitListenerAttribute rabbitListener, MethodInfo method, object bean, string beanName)
        {
            var endpoint = new MethodRabbitListenerEndpoint(ApplicationContext, method, bean, _logger);
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
                            factoryTarget + "], no IRabbitListenerContainerFactory with id '" + name + "' was found");
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

                endpoint.Admin = ApplicationContext.GetService<IAmqpAdmin>(rabbitAdmin);
                if (endpoint.Admin == null)
                {
                    throw new InvalidOperationException("Could not register rabbit listener endpoint on [" +
                            adminTarget + "], no RabbitAdmin with id '" + rabbitAdmin + "' was found");
                }
            }
        }

        private void ResolveAckMode(MethodRabbitListenerEndpoint endpoint, RabbitListenerAttribute rabbitListener)
        {
            var ackModeAttr = rabbitListener.AckMode;
            if (!string.IsNullOrEmpty(ackModeAttr))
            {
                var ackMode = ResolveExpression(ackModeAttr);
                if (ackMode is string)
                {
                    endpoint.AckMode = (AcknowledgeMode)Enum.Parse(typeof(AcknowledgeMode), (string)ackMode);
                }
                else if (ackMode is AcknowledgeMode)
                {
                    endpoint.AckMode = (AcknowledgeMode)ackMode;
                }
                else
                {
                    throw new InvalidOperationException("AckMode must resolve to a String or AcknowledgeMode");
                }
            }
        }

        private bool ResolveExpressionAsBoolean(string value, bool defaultValue = false)
        {
            var resolved = ResolveExpression(value);
            if (resolved is bool)
            {
                return (bool)resolved;
            }
            else if (resolved is string)
            {
                if (bool.TryParse((string)resolved, out var result))
                {
                    return result;
                }

                return defaultValue;
            }
            else
            {
                return defaultValue;
            }
        }

        private int? ResolveExpressionAsInteger(string value, string attribute)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            var resolved = ResolveExpression(value);
            if (resolved is string && int.TryParse((string)resolved, out var result))
            {
                return result;
            }
            else if (resolved is int)
            {
                return (int)resolved;
            }
            else
            {
                throw new InvalidOperationException("The [" + attribute + "] must resolve to a int?. Resolved to [" + resolved.GetType() + "] for [" + value + "]");
            }
        }

        private string[] ResolveQueues(RabbitListenerAttribute rabbitListener)
        {
            var queues = rabbitListener.Queues;

            var result = new List<string>();
            if (queues.Length > 0)
            {
                for (var i = 0; i < queues.Length; i++)
                {
                    ResolveAsString(ResolveExpression(queues[i]), result, true, "Queues");
                }
            }

            return result.ToArray();
        }

        private void ResolveAsString(object resolvedValue, List<string> result, bool canBeQueue, string what)
        {
            var resolvedValueToUse = resolvedValue;
            if (resolvedValue is string[])
            {
                resolvedValueToUse = new List<string>((string[])resolvedValue);
            }

            if (canBeQueue && resolvedValueToUse is Config.Queue)
            {
                result.Add(((Config.Queue)resolvedValueToUse).Name);
            }
            else if (resolvedValueToUse is string)
            {
                result.Add((string)resolvedValueToUse);
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
                    string.Format("RabbitListenerAttribute." + what + " can't resolve {0} as a String[] or a String " + (canBeQueue ? "or a Queue" : string.Empty), resolvedValue));
            }
        }

        private object ResolveExpression(string value)
        {
            var resolvedValue = Resolve(value);

            // TODO: This needs to handle #{} and !{} with beanreferences '@'
            if (Resolver != null && ExpressionContext != null)
            {
                return Resolver.Evaluate(resolvedValue, ExpressionContext);
            }

            return resolvedValue;
        }

        private string Resolve(string value)
        {
            if (ApplicationContext != null)
            {
                var config = ApplicationContext.Configuration;
                if (config != null)
                {
                    return PropertyPlaceholderHelper.ResolvePlaceholders(value, config, _logger);
                }
            }

            return value;
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
                return ApplicationContext.ServiceProvider.GetService(implementation);
            }
            catch (Exception e)
            {
                // Log
                throw new InvalidOperationException("Unable to CreateInstance of type containing RabbitListener method, Type: " + implementation, e);
            }
        }

        private class RabbitHandlerMethodFactoryAdapter : IMessageHandlerMethodFactory
        {
            private readonly RabbitListenerAttributeProcessor _processor;
            private IMessageHandlerMethodFactory _factory;

            public IMessageHandlerMethodFactory Factory
            {
                get
                {
                    if (_factory == null)
                    {
                        _factory = CreateDefaultMessageHandlerMethodFactory();
                    }

                    return _factory;
                }

                set
                {
                    _factory = value;
                }
            }

            public RabbitHandlerMethodFactoryAdapter(RabbitListenerAttributeProcessor processor)
            {
                _processor = processor;
            }

            public IInvocableHandlerMethod CreateInvocableHandlerMethod(object bean, MethodInfo method)
            {
                return Factory.CreateInvocableHandlerMethod(bean, method);
            }

            private IMessageHandlerMethodFactory CreateDefaultMessageHandlerMethodFactory()
            {
                var defaultFactory = new DefaultMessageHandlerMethodFactory();
                var conversionService = new DefaultConversionService();
                conversionService.AddConverter(new BytesToStringConverter(_processor.Charset));
                defaultFactory.ConversionService = conversionService;
                return defaultFactory;
            }
        }

        private class BytesToStringConverter : IGenericConverter
        {
            private readonly Encoding _charset;

            public BytesToStringConverter(Encoding charset)
            {
                _charset = charset;
                ConvertibleTypes = new HashSet<(Type Source, Type Target)>() { (typeof(byte[]), typeof(string)) };
            }

            public ISet<(Type Source, Type Target)> ConvertibleTypes { get; }

            public object Convert(object source, Type sourceType, Type targetType)
            {
                if (!(source is byte[] asByteArray))
                {
                    return null;
                }

                return _charset.GetString(asByteArray);
            }
        }
    }
}
