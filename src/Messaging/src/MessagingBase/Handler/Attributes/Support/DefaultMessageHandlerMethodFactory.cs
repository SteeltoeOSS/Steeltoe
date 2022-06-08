// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Converter;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Invocation;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Attributes.Support;

public class DefaultMessageHandlerMethodFactory : IMessageHandlerMethodFactory
{
    public const string DEFAULT_SERVICE_NAME = nameof(DefaultMessageHandlerMethodFactory);

    protected readonly HandlerMethodArgumentResolverComposite _argumentResolvers = new ();

    public virtual string ServiceName { get; set; } = DEFAULT_SERVICE_NAME;

    public virtual IConversionService ConversionService { get; set; }

    public virtual IMessageConverter MessageConverter { get; set; }

    public virtual List<IHandlerMethodArgumentResolver> CustomArgumentResolvers { get; set; }

    public virtual IApplicationContext ApplicationContext { get; set; }

    public DefaultMessageHandlerMethodFactory(IApplicationContext context = null)
        : this(null, null, null, context)
    {
    }

    public DefaultMessageHandlerMethodFactory(IConversionService conversionService, IApplicationContext context = null)
        : this(conversionService, null, null, context)
    {
        ConversionService = conversionService;
    }

    public DefaultMessageHandlerMethodFactory(IConversionService conversionService, IMessageConverter converter, IApplicationContext context = null)
        : this(conversionService, converter, null, context)
    {
        ConversionService = conversionService;
        MessageConverter = converter;
    }

    public DefaultMessageHandlerMethodFactory(IConversionService conversionService, IMessageConverter converter, List<IHandlerMethodArgumentResolver> resolvers, IApplicationContext context = null)
    {
        ConversionService = conversionService;
        MessageConverter = converter;
        CustomArgumentResolvers = resolvers;

        ConversionService ??= new GenericConversionService();

        MessageConverter ??= new GenericMessageConverter(ConversionService);

        if (_argumentResolvers.Resolvers.Count == 0)
        {
            _argumentResolvers.AddResolvers(InitArgumentResolvers());
        }

        ApplicationContext = context;
    }

    public virtual void SetArgumentResolvers(List<IHandlerMethodArgumentResolver> argumentResolvers)
    {
        if (argumentResolvers == null)
        {
            _argumentResolvers.Clear();
            return;
        }

        if (argumentResolvers.Count > 0)
        {
            _argumentResolvers.Clear();
        }

        _argumentResolvers.AddResolvers(argumentResolvers);
    }

    public virtual IInvocableHandlerMethod CreateInvocableHandlerMethod(object bean, MethodInfo method)
    {
        var handlerMethod = new InvocableHandlerMethod(bean, method)
        {
            MessageMethodArgumentResolvers = _argumentResolvers
        };
        return handlerMethod;
    }

    public virtual void Initialize()
    {
        _argumentResolvers.Clear();

        ConversionService ??= new GenericConversionService();

        MessageConverter ??= new GenericMessageConverter(ConversionService);

        if (_argumentResolvers.Resolvers.Count == 0)
        {
            _argumentResolvers.AddResolvers(InitArgumentResolvers());
        }
    }

    protected List<IHandlerMethodArgumentResolver> InitArgumentResolvers()
    {
        var resolvers = new List<IHandlerMethodArgumentResolver>
        {
            // Annotation-based argument resolution
            new HeaderMethodArgumentResolver(ConversionService, ApplicationContext),
            new HeadersMethodArgumentResolver(),

            // Type-based argument resolution
            new MessageMethodArgumentResolver(MessageConverter)
        };

        if (CustomArgumentResolvers != null)
        {
            resolvers.AddRange(CustomArgumentResolvers);
        }

        resolvers.Add(new PayloadMethodArgumentResolver(MessageConverter));

        return resolvers;
    }
}
