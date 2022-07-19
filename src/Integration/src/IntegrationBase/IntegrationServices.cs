// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Converter;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Support;
using Steeltoe.Integration.Support.Channel;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;

namespace Steeltoe.Integration;

public class IntegrationServices : IIntegrationServices
{
    protected IMessageBuilderFactory messageBuilderFactory;
    protected IConversionService conversionService;
    protected IIdGenerator idGenerator;
    protected IDestinationResolver<IMessageChannel> channelResolver;
    protected IApplicationContext context;
    protected IExpressionParser expressionParser;

    public IntegrationServices(IApplicationContext context)
    {
        this.context = context;
    }

    public virtual IMessageBuilderFactory MessageBuilderFactory
    {
        get
        {
            messageBuilderFactory ??= context?.GetService<IMessageBuilderFactory>() ?? new DefaultMessageBuilderFactory();
            return messageBuilderFactory;
        }

        set
        {
            messageBuilderFactory = value;
        }
    }

    public virtual IExpressionParser ExpressionParser
    {
        get
        {
            expressionParser ??= context?.GetService<IExpressionParser>() ?? new SpelExpressionParser();
            return expressionParser;
        }

        set
        {
            expressionParser = value;
        }
    }

    public virtual IDestinationResolver<IMessageChannel> ChannelResolver
    {
        get
        {
            channelResolver ??= context?.GetService<IDestinationResolver<IMessageChannel>>() ?? new DefaultMessageChannelResolver(context);
            return channelResolver;
        }

        set
        {
            channelResolver = value;
        }
    }

    public virtual IConversionService ConversionService
    {
        get
        {
            conversionService ??= context?.GetService<IConversionService>() ?? DefaultConversionService.Singleton;
            return conversionService;
        }

        set
        {
            conversionService = value;
        }
    }

    public virtual IIdGenerator IdGenerator
    {
        get
        {
            idGenerator ??= context?.GetService<IIdGenerator>() ?? new DefaultIdGenerator();
            return idGenerator;
        }

        set
        {
            idGenerator = value;
        }
    }
}
