// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Converter;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Handler.Support;
using Steeltoe.Integration.Support;
using Steeltoe.Integration.Support.Converter;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Messaging.Handler.Invocation;

namespace Steeltoe.Integration.Handler;

public class MethodInvokingMessageProcessor<T> : AbstractMessageProcessor<T>, ILifecycle
{
    private readonly IInvocableHandlerMethod _invocableHandlerMethod;
    private IConversionService _conversionService;

    public virtual IConversionService ConversionService
    {
        get
        {
            _conversionService ??= IntegrationServices.ConversionService;
            return _conversionService;
        }

        set => _conversionService = value;
    }

    public bool IsRunning { get; private set; }

    public MethodInvokingMessageProcessor(IApplicationContext context, object targetObject, MethodInfo method)
        : base(context)
    {
        IMessageHandlerMethodFactory messageHandlerMethodFactory = ConfigureMessageHandlerFactory();
        _invocableHandlerMethod = messageHandlerMethodFactory.CreateInvocableHandlerMethod(targetObject, method);
    }

    public MethodInvokingMessageProcessor(IApplicationContext context, object targetObject, Type attribute)
        : base(context)
    {
        MethodInfo method = FindAnnotatedMethod(targetObject, attribute);
        IMessageHandlerMethodFactory messageHandlerMethodFactory = ConfigureMessageHandlerFactory();
        _invocableHandlerMethod = messageHandlerMethodFactory.CreateInvocableHandlerMethod(targetObject, method);
    }

    public Task StartAsync()
    {
        IsRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        IsRunning = false;
        return Task.CompletedTask;
    }

    public override T ProcessMessage(IMessage message)
    {
        try
        {
            object result = _invocableHandlerMethod.Invoke(message);
#pragma warning disable S2219 // Runtime type checking should be simplified
            if (result != null && typeof(T).IsAssignableFrom(result.GetType()))
#pragma warning restore S2219 // Runtime type checking should be simplified
            {
                return (T)ConversionService.Convert(result, result?.GetType(), typeof(T));
            }

            return (T)result;
        }
        catch (Exception e)
        {
            throw new MessageHandlingException(message, e);
        }
    }

    private static MethodInfo FindAnnotatedMethod(object target, Type attribute)
    {
        List<MethodInfo> results = AttributeUtils.FindMethodsWithAttribute(
            target.GetType(), attribute, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (results.Count != 1)
        {
            throw new InvalidOperationException($"Multiple methods with attribute: {attribute} exist on type: {target.GetType()}");
        }

        return results[0];
    }

    private IMessageHandlerMethodFactory ConfigureMessageHandlerFactory()
    {
        IMessageHandlerMethodFactory factory = ApplicationContext?.GetService<IMessageHandlerMethodFactory>() ?? ConfigureLocalMessageHandlerFactory();
        return factory;
    }

    private IMessageHandlerMethodFactory ConfigureLocalMessageHandlerFactory()
    {
        var factory = new DefaultMessageHandlerMethodFactory(ConversionService, ApplicationContext);

        var messageConverter = ApplicationContext?.GetService<IMessageConverter>(IntegrationContextUtils.ArgumentResolverMessageConverterBeanName);

        if (messageConverter != null)
        {
            factory.MessageConverter = messageConverter;
        }
        else
        {
            messageConverter = new ConfigurableCompositeMessageConverter(ConversionService);
        }

        var payloadExpressionArgumentResolver = new PayloadExpressionArgumentResolver(ApplicationContext);
        var payloadsArgumentResolver = new PayloadsArgumentResolver(ApplicationContext);
        var nullResolver = new NullAwarePayloadArgumentResolver(messageConverter);

        var customArgumentResolvers = new List<IHandlerMethodArgumentResolver>
        {
            payloadExpressionArgumentResolver,
            nullResolver,
            payloadsArgumentResolver
        };

        var mapArgumentResolver = new DictionaryArgumentResolver(ApplicationContext);
        customArgumentResolvers.Add(mapArgumentResolver);

        factory.CustomArgumentResolvers = customArgumentResolvers;
        factory.Initialize();
        return factory;
    }
}
