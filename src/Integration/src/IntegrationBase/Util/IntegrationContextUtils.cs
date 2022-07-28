// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Messaging;

namespace Steeltoe.Integration.Util;

public static class IntegrationContextUtils
{
    public const string NullChannelBeanName = "nullChannel";

    public const string ErrorChannelBeanName = "errorChannel";

    public const string IntegrationEvaluationContextBeanName = "integrationEvaluationContext";

    public const string IntegrationSimpleEvaluationContextBeanName = "integrationSimpleEvaluationContext";

    public const string MessageHandlerFactoryBeanName = "integrationMessageHandlerMethodFactory";

    public const string ArgumentResolverMessageConverterBeanName = "integrationArgumentResolverMessageConverter";

    public static IMessageChannel GetErrorChannel(IApplicationContext context)
    {
        return context.GetService<IMessageChannel>(ErrorChannelBeanName);
    }

    public static IMessageChannel GetNullChannel(IApplicationContext context)
    {
        return context.GetService<IMessageChannel>(NullChannelBeanName);
    }

    public static IEvaluationContext GetEvaluationContext(IApplicationContext context)
    {
        return context.GetService<IEvaluationContext>(IntegrationEvaluationContextBeanName);
    }

    public static IEvaluationContext GetSimpleEvaluationContext(IApplicationContext context)
    {
        return context.GetService<IEvaluationContext>(IntegrationSimpleEvaluationContextBeanName);
    }

    public static IExpressionParser GetExpressionParser(IApplicationContext context)
    {
        var result = context.GetService<IExpressionParser>() ?? new SpelExpressionParser();

        return result;
    }
}
