// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Attributes;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Services;
using Steeltoe.Integration.Attributes;
using Steeltoe.Integration.Endpoint;
using Steeltoe.Integration.Handler;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Steeltoe.Integration.Config
{
    public class ServiceActivatorAttributeProcessor : AbstractMethodAttributeProcessor<ServiceActivatorAttribute>
    {
        private readonly List<IServiceActivatorMethod> _serviceActivatorMethods;

        public ServiceActivatorAttributeProcessor(IApplicationContext applicationContext, IEnumerable<IServiceActivatorMethod> methods)
            : base(applicationContext)
        {
            MessageHandlerProperties.AddRange(new List<string>() { "OutputChannel", "RequiresReply" });
            _serviceActivatorMethods = methods.ToList();
        }

        public void Initialize()
        {
            if (_serviceActivatorMethods.Count <= 0)
            {
                return;
            }

            foreach (var method in _serviceActivatorMethods)
            {
                var service = CreateTargetService(method.ImplementationType);
                if (service != null)
                {
                    var attributes = method.Method.GetCustomAttributes().ToList();
                    var serviceName = GetServiceName(service);
                    var result = PostProcess(service, serviceName, method.Method, attributes);
                    if (result is AbstractEndpoint)
                    {
                        var endpoint = (AbstractEndpoint)result;
                        var autoStartup = MessagingAttributeUtils.ResolveAttribute<string>(attributes, "AutoStartup");
                        if (!string.IsNullOrEmpty(autoStartup))
                        {
                            autoStartup = ApplicationContext?.ResolveEmbeddedValue(autoStartup);
                            if (!string.IsNullOrEmpty(autoStartup))
                            {
                                endpoint.IsAutoStartup = bool.Parse(autoStartup);
                            }
                        }

                        var phase = MessagingAttributeUtils.ResolveAttribute<string>(attributes, "Phase");
                        if (!string.IsNullOrEmpty(phase))
                        {
                            phase = ApplicationContext?.ResolveEmbeddedValue(phase);
                            if (!string.IsNullOrEmpty(phase))
                            {
                                endpoint.Phase = int.Parse(phase);
                            }
                        }

                        var endpointName = GenerateServiceName(serviceName, method.Method, typeof(ServiceActivatorAttribute));
                        endpoint.ServiceName = endpointName;
                        ApplicationContext?.Register(endpointName, endpoint);
                    }
                }
            }
        }

        protected override IMessageHandler CreateHandler(object service, MethodInfo method, List<Attribute> attributes)
        {
            AbstractReplyProducingMessageHandler serviceActivator;
            if (method.GetCustomAttribute<ServiceAttribute>() != null)
            {
                // Service Attribute usage
                var target = ResolveTargetServiceFromMethodWithServiceAnnotation(method);
                serviceActivator = ExtractTypeIfPossible<AbstractReplyProducingMessageHandler>(target);
                if (serviceActivator == null)
                {
                    if (target is IMessageHandler handler)
                    {
                        /*
                         * Return a reply-producing message handler so that we still get 'produced no reply' messages
                         * and the super class will inject the advice chain to advise the handler method if needed.
                         */
                        return new ReplyProducingMessageHandlerWrapper(ApplicationContext, handler);
                    }
                    else
                    {
                        serviceActivator = new ServiceActivatingHandler(ApplicationContext, target, method);
                    }
                }
                else
                {
                    CheckMessageHandlerAttributes(ResolveTargetServiceName(method), attributes);
                    return (IMessageHandler)target;
                }
            }
            else
            {
                serviceActivator = new ServiceActivatingHandler(ApplicationContext, service, method);
            }

            var requiresReply = MessagingAttributeUtils.ResolveAttribute<string>(attributes, "RequiresReply");
            if (!string.IsNullOrEmpty(requiresReply))
            {
                serviceActivator.RequiresReply = bool.Parse(ApplicationContext.ResolveEmbeddedValue(requiresReply));
            }

            SetOutputChannelIfPresent(attributes, serviceActivator);
            return serviceActivator;
        }

        protected virtual string GenerateServiceName(string originalServiceName, MethodInfo method, Type attributeType)
        {
            var name = MessagingAttributeUtils.EndpointIdValue(method);
            if (string.IsNullOrEmpty(name))
            {
                name = originalServiceName + "." + method.Name + "." + attributeType.Name + "." + Guid.NewGuid().ToString();
            }

            return name;
        }

        private string GetServiceName(object service)
        {
            if (service is IServiceNameAware aware)
            {
                return aware.ServiceName;
            }

            return service.GetType().FullName;
        }

        private object CreateTargetService(Type implementation)
        {
            try
            {
                return ApplicationContext.GetService(implementation);
            }
            catch (Exception e)
            {
                // Log
                throw new InvalidOperationException("Unable to CreateInstance of type containing StreamListener method, Type: " + implementation, e);
            }
        }
    }
}
