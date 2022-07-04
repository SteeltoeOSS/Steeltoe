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
    protected IMessageBuilderFactory innerMessageBuilderFactory;
    protected IConversionService innerConversionService;
    protected IIdGenerator innerIdGenerator;
    protected IDestinationResolver<IMessageChannel> innerChannelResolver;
    protected IApplicationContext context;
    protected IExpressionParser innerExpressionParser;

    public IntegrationServices(IApplicationContext context)
    {
        this.context = context;
    }

    public virtual IMessageBuilderFactory MessageBuilderFactory
    {
        get
        {
            innerMessageBuilderFactory ??= context?.GetService<IMessageBuilderFactory>() ?? new DefaultMessageBuilderFactory();
            return innerMessageBuilderFactory;
        }

        set
        {
            innerMessageBuilderFactory = value;
        }
    }

    public virtual IExpressionParser ExpressionParser
    {
        get
        {
            innerExpressionParser ??= context?.GetService<IExpressionParser>() ?? new SpelExpressionParser();
            return innerExpressionParser;
        }

        set
        {
            innerExpressionParser = value;
        }
    }

    public virtual IDestinationResolver<IMessageChannel> ChannelResolver
    {
        get
        {
            innerChannelResolver ??= context?.GetService<IDestinationResolver<IMessageChannel>>() ?? new DefaultMessageChannelResolver(context);
            return innerChannelResolver;
        }

        set
        {
            innerChannelResolver = value;
        }
    }

    public virtual IConversionService ConversionService
    {
        get
        {
            innerConversionService ??= context?.GetService<IConversionService>() ?? DefaultConversionService.Singleton;
            return innerConversionService;
        }

        set
        {
            innerConversionService = value;
        }
    }

    public virtual IIdGenerator IdGenerator
    {
        get
        {
            innerIdGenerator ??= context?.GetService<IIdGenerator>() ?? new DefaultIdGenerator();
            return innerIdGenerator;
        }

        set
        {
            innerIdGenerator = value;
        }
    }
}
