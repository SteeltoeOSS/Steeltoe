// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Steeltoe.Integration.Handler;

public class MethodInvokingMessageProcessor<T> : AbstractMessageProcessor<T>, ILifecycle
{
    private readonly IInvocableHandlerMethod _invocableHandlerMethod;
    private IConversionService _conversionService;

    public MethodInvokingMessageProcessor(IApplicationContext context, object targetObject, MethodInfo method)
        : base(context)
    {
        var messageHandlerMethodFactory = ConfigureMessageHandlerFactory();
        _invocableHandlerMethod = messageHandlerMethodFactory.CreateInvocableHandlerMethod(targetObject, method);
    }

    public MethodInvokingMessageProcessor(IApplicationContext context, object targetObject, Type attribute)
        : base(context)
    {
        var method = FindAnnotatedMethod(targetObject, attribute);
        var messageHandlerMethodFactory = ConfigureMessageHandlerFactory();
        _invocableHandlerMethod = messageHandlerMethodFactory.CreateInvocableHandlerMethod(targetObject, method);
    }

    public virtual IConversionService ConversionService
    {
        get
        {
            _conversionService ??= IntegrationServices.ConversionService;
            return _conversionService;
        }

        set
        {
            _conversionService = value;
        }
    }

    public bool IsRunning { get; private set; }

    public Task Start()
    {
        IsRunning = true;
        return Task.CompletedTask;
    }

    public Task Stop()
    {
        IsRunning = false;
        return Task.CompletedTask;
    }

    public override T ProcessMessage(IMessage message)
    {
        try
        {
            var result = _invocableHandlerMethod.Invoke(message);
#pragma warning disable S2219 // Runtime type checking should be simplified
            if (result != null && typeof(T).IsAssignableFrom(result.GetType()))
#pragma warning restore S2219 // Runtime type checking should be simplified
            {
                return (T)ConversionService.Convert(result, result?.GetType(), typeof(T));
            }
            else
            {
                return (T)result;
            }
        }
        catch (Exception e)
        {
            throw new MessageHandlingException(message, e);
        }
    }

    private static MethodInfo FindAnnotatedMethod(object target, Type attribute)
    {
        var results = AttributeUtils.FindMethodsWithAttribute(
            target.GetType(),
            attribute,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (results.Count != 1)
        {
            throw new InvalidOperationException($"Multiple methods with attribute: {attribute} exist on type: {target.GetType()}");
        }

        return results[0];
    }

    private IMessageHandlerMethodFactory ConfigureMessageHandlerFactory()
    {
        var factory = ApplicationContext?.GetService<IMessageHandlerMethodFactory>() ?? ConfigureLocalMessageHandlerFactory();
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
            payloadExpressionArgumentResolver, nullResolver, payloadsArgumentResolver
        };

        var mapArgumentResolver = new DictionaryArgumentResolver(ApplicationContext);
        customArgumentResolvers.Add(mapArgumentResolver);

        factory.CustomArgumentResolvers = customArgumentResolvers;
        factory.Initialize();
        return factory;
    }
}
