// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Attributes;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Services;
using Steeltoe.Integration.Attributes;
using Steeltoe.Integration.Endpoint;
using Steeltoe.Integration.Handler;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;

namespace Steeltoe.Integration.Config;

public class ServiceActivatorAttributeProcessor : AbstractMethodAttributeProcessor<ServiceActivatorAttribute>
{
    private readonly List<IServiceActivatorMethod> _serviceActivatorMethods;

    public ServiceActivatorAttributeProcessor(IApplicationContext applicationContext, IEnumerable<IServiceActivatorMethod> methods,
        ILogger<ServiceActivatorAttributeProcessor> logger)
        : base(applicationContext, logger)
    {
        MessageHandlerProperties.Add("OutputChannel");
        MessageHandlerProperties.Add("RequiresReply");

        _serviceActivatorMethods = methods.ToList();
    }

    public void Initialize()
    {
        if (_serviceActivatorMethods.Count <= 0)
        {
            return;
        }

        foreach (IServiceActivatorMethod method in _serviceActivatorMethods)
        {
            object service = CreateTargetService(method.ImplementationType);

            if (service == null)
            {
                continue;
            }

            List<Attribute> attributes = method.Method.GetCustomAttributes().ToList();
            string serviceName = GetServiceName(service);
            object result = PostProcess(service, serviceName, method.Method, attributes);

            AbstractEndpoint endpoint = GetEndpoint(attributes, result);

            if (endpoint == null)
            {
                continue;
            }

            string endpointName = GenerateServiceName(serviceName, method.Method, typeof(ServiceActivatorAttribute));
            endpoint.ServiceName = endpointName;
            ApplicationContext?.Register(endpointName, endpoint);
        }
    }

    protected override IMessageHandler CreateHandler(object service, MethodInfo method, ICollection<Attribute> attributes)
    {
        AbstractReplyProducingMessageHandler serviceActivator;

        if (method.GetCustomAttribute<ServiceAttribute>() != null)
        {
            // Service Attribute usage
            object target = ResolveTargetServiceFromMethodWithServiceAnnotation(method);
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

                serviceActivator = new ServiceActivatingHandler(ApplicationContext, target, method);
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

        string requiresReply = MessagingAttributeUtils.ResolveAttribute<string>(attributes, "RequiresReply");

        if (!string.IsNullOrEmpty(requiresReply))
        {
            serviceActivator.RequiresReply = bool.Parse(ApplicationContext.ResolveEmbeddedValue(requiresReply));
        }

        SetOutputChannelIfPresent(attributes, serviceActivator);
        return serviceActivator;
    }

    protected virtual string GenerateServiceName(string originalServiceName, MethodInfo method, Type attributeType)
    {
        string name = MessagingAttributeUtils.EndpointIdValue(method);

        if (string.IsNullOrEmpty(name))
        {
            name = $"{originalServiceName}.{method.Name}.{attributeType.Name}.{Guid.NewGuid()}";
        }

        return name;
    }

    private static string GetServiceName(object service)
    {
        if (service is IServiceNameAware aware)
        {
            return aware.ServiceName;
        }

        return service.GetType().FullName;
    }

    private AbstractEndpoint GetEndpoint(List<Attribute> attributes, object result)
    {
        var endpoint = result as AbstractEndpoint;

        if (endpoint != null)
        {
            string autoStartup = MessagingAttributeUtils.ResolveAttribute<string>(attributes, "AutoStartup");

            if (!string.IsNullOrEmpty(autoStartup))
            {
                autoStartup = ApplicationContext?.ResolveEmbeddedValue(autoStartup);

                if (!string.IsNullOrEmpty(autoStartup))
                {
                    endpoint.IsAutoStartup = bool.Parse(autoStartup);
                }
            }

            string phase = MessagingAttributeUtils.ResolveAttribute<string>(attributes, "Phase");

            if (!string.IsNullOrEmpty(phase))
            {
                phase = ApplicationContext?.ResolveEmbeddedValue(phase);

                if (!string.IsNullOrEmpty(phase))
                {
                    endpoint.Phase = int.Parse(phase);
                }
            }
        }

        return endpoint;
    }

    private object CreateTargetService(Type implementationType)
    {
        try
        {
            return ApplicationContext.GetService(implementationType);
        }
        catch (Exception e)
        {
            // Log
            throw new InvalidOperationException($"Unable to CreateInstance of type containing StreamListener method, Type: {implementationType}", e);
        }
    }
}
