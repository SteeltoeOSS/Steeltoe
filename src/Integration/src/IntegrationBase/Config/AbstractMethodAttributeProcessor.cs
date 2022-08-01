// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Attributes;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Converter;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Endpoint;
using Steeltoe.Integration.Handler;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using System.Reflection;

namespace Steeltoe.Integration.Config;

public abstract class AbstractMethodAttributeProcessor<TAttribute> : IMethodAttributeProcessor<TAttribute>
    where TAttribute : Attribute
{
    protected const string SendTimeoutPropertyName = "SendTimeout";
    protected const string InputChannelPropertyName = "InputChannel";
    private readonly ILogger _logger;

    protected virtual List<string> MessageHandlerProperties { get; } = new ();

    protected IApplicationContext ApplicationContext { get; }

    protected virtual IConversionService ConversionService { get; }

    protected virtual IDestinationResolver<IMessageChannel> ChannelResolver { get; }

    protected virtual Type AnnotationType { get; }

    protected virtual string InputChannelProperty { get; } = InputChannelPropertyName;

    protected AbstractMethodAttributeProcessor(IApplicationContext applicationContext, ILogger logger)
    {
        ApplicationContext = applicationContext ?? throw new ArgumentNullException(nameof(applicationContext));
        _logger = logger;
        MessageHandlerProperties.Add(SendTimeoutPropertyName);
        ConversionService = ApplicationContext.GetService<IConversionService>() ?? DefaultConversionService.Singleton;

        ChannelResolver = new DefaultMessageChannelDestinationResolver(applicationContext);
        AnnotationType = typeof(TAttribute);
    }

    public object PostProcess(object service, string serviceName, MethodInfo method, List<Attribute> attributes)
    {
        GetSourceHandlerFromContext(method, out var sourceHandler, out var skipEndpointCreation);

        if (skipEndpointCreation)
        {
            return null;
        }

        var handler = GetHandler(service, method, attributes);

        if (handler != sourceHandler)
        {
            var handlerServiceName = GenerateHandlerServiceName(serviceName, method);
            if (handler is ReplyProducingMessageHandlerWrapper && !string.IsNullOrEmpty(MessagingAttributeUtils.EndpointIdValue(method)))
            {
                handlerServiceName += ".wrapper";
            }

            ApplicationContext.Register(handlerServiceName, handler);
            handler = (IMessageHandler)ApplicationContext.GetService(handlerServiceName);
        }

        var endpoint = CreateEndpoint(handler, method, attributes);
        if (endpoint != null)
        {
            return endpoint;
        }
        else
        {
            return handler;
        }
    }

    public virtual bool ShouldCreateEndpoint(MethodInfo method, List<Attribute> attributes)
    {
        var inputChannel = MessagingAttributeUtils.ResolveAttribute<string>(attributes, InputChannelProperty);
        var createEndpoint = !string.IsNullOrEmpty(inputChannel);
        if (!createEndpoint && ServiceAnnotationAware())
        {
            var isService = method.GetCustomAttribute<ServiceAttribute>() != null;
            if (isService)
            {
                throw new InvalidOperationException($"A channel name in '{InputChannelProperty}' is required when {AnnotationType} is used on '[Service]' methods.");
            }
        }

        return createEndpoint;
    }

    protected virtual bool ServiceAnnotationAware()
    {
        return false;
    }

    protected virtual AbstractEndpoint CreateEndpoint(IMessageHandler handler, MethodInfo method, List<Attribute> annotations)
    {
        AbstractEndpoint endpoint = null;
        var inputChannelName = MessagingAttributeUtils.ResolveAttribute<string>(annotations, InputChannelProperty);
        if (!string.IsNullOrEmpty(inputChannelName))
        {
            IMessageChannel inputChannel;
            try
            {
                inputChannel = ChannelResolver.ResolveDestination(inputChannelName);
                if (inputChannel == null)
                {
                    inputChannel = new DirectChannel(ApplicationContext, inputChannelName);
                    ApplicationContext.Register(inputChannelName, inputChannel);
                }
            }
            catch (DestinationResolutionException)
            {
                inputChannel = new DirectChannel(ApplicationContext, inputChannelName);
                ApplicationContext.Register(inputChannelName, inputChannel);
            }

            endpoint = DoCreateEndpoint(handler, inputChannel, annotations);
        }

        return endpoint;
    }

    protected virtual AbstractEndpoint DoCreateEndpoint(IMessageHandler handler, IMessageChannel inputChannel, List<Attribute> annotations)
    {
        if (inputChannel is IPollableChannel)
        {
            throw new InvalidOperationException("No support for IPollableChannel");
        }

        return new EventDrivenConsumerEndpoint(ApplicationContext, (ISubscribableChannel)inputChannel, handler);
    }

    protected virtual string GenerateHandlerServiceName(string originalServiceName, MethodInfo method)
    {
        var name = MessagingAttributeUtils.EndpointIdValue(method);
        if (string.IsNullOrEmpty(name))
        {
            originalServiceName ??= method.DeclaringType.Name;

            var baseName = $"{originalServiceName}.{method.Name}.{AnnotationType.Name}";
            name = baseName;
            var count = 1;
            while (ApplicationContext.ContainsService(name))
            {
                name = $"{baseName}#{++count}";
            }
        }

        return $"{name}.handler";
    }

    protected virtual void SetOutputChannelIfPresent(List<Attribute> annotations, AbstractReplyProducingMessageHandler handler)
    {
        var outputChannelName = MessagingAttributeUtils.ResolveAttribute<string>(annotations, "OutputChannel");
        if (!string.IsNullOrEmpty(outputChannelName))
        {
            handler.OutputChannelName = outputChannelName;
        }
    }

    protected virtual object ResolveTargetServiceFromMethodWithServiceAnnotation(MethodInfo method)
    {
        var id = ResolveTargetServiceName(method);
        return ApplicationContext.GetService(id);
    }

    protected virtual string ResolveTargetServiceName(MethodInfo method)
    {
        var id = method.Name;
        var name = method.GetCustomAttribute<ServiceAttribute>().Name;
        if (string.IsNullOrEmpty(name))
        {
            return id;
        }

        return name;
    }

    protected virtual T ExtractTypeIfPossible<T>(object targetObject)
    {
        if (targetObject == null)
        {
            return default;
        }

        if (targetObject is T h)
        {
            return h;
        }

        return default;
    }

    protected void CheckMessageHandlerAttributes(string handlerServiceName, List<Attribute> annotations)
    {
        foreach (var property in MessageHandlerProperties)
        {
            foreach (var annotation in annotations)
            {
                var value = AttributeUtils.GetValue(annotation, property);
                if (MessagingAttributeUtils.HasValue(value))
                {
                    throw new InvalidOperationException(
                        $"The IMessageHandler [{handlerServiceName}] can not be populated because of ambiguity with attribute properties {string.Join(',', MessageHandlerProperties)} which are not allowed when an integration attribute is used with a service definition for a IMessageHandler.\n" +
                        $"The property causing the ambiguity is: [{property}].\n" +
                        "Use the appropriate setter on the IMessageHandler directly when configuring an endpoint this way.");
                }
            }
        }
    }

    protected abstract IMessageHandler CreateHandler(object service, MethodInfo method, List<Attribute> attributes);

    private IMessageHandler GetHandler(object service, MethodInfo method, List<Attribute> attributes)
    {
        var handler = CreateHandler(service, method, attributes);

        if (handler is AbstractMessageProducingHandler)
        {
            var sendTimeout = MessagingAttributeUtils.ResolveAttribute<string>(attributes, "SendTimeout");
            if (sendTimeout != null)
            {
                var resolvedValue = ApplicationContext.ResolveEmbeddedValue(sendTimeout);
                if (resolvedValue != null)
                {
                    var value = int.Parse(resolvedValue);
                    if (handler is AbstractMessageProducingHandler abstractHandler)
                    {
                        abstractHandler.SendTimeout = value;
                    }
                }
            }
        }

        return handler;
    }

    private void GetSourceHandlerFromContext(MethodInfo method, out object sourceHandler, out bool skipEndpointCreation)
    {
        skipEndpointCreation = false;
        sourceHandler = null;
        if (ServiceAnnotationAware() && method.GetCustomAttribute<ServiceAttribute>() != null)
        {
            if (!ApplicationContext.ContainsService(ResolveTargetServiceName(method)))
            {
                _logger?.LogDebug("Skipping endpoint creation; perhaps due to some '[Conditional]' attribute.");
                skipEndpointCreation = true;
            }
            else
            {
                sourceHandler = ResolveTargetServiceFromMethodWithServiceAnnotation(method);
            }
        }
    }
}
