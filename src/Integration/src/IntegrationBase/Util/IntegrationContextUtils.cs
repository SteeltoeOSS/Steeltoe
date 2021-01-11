// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Messaging;

namespace Steeltoe.Integration.Util
{
    public static class IntegrationContextUtils
    {
        public const string NULL_CHANNEL_BEAN_NAME = "nullChannel";

        public const string ERROR_CHANNEL_BEAN_NAME = "errorChannel";

        public const string INTEGRATION_EVALUATION_CONTEXT_BEAN_NAME = "integrationEvaluationContext";

        public const string INTEGRATION_SIMPLE_EVALUATION_CONTEXT_BEAN_NAME = "integrationSimpleEvaluationContext";

        public static IMessageChannel GetErrorChannel(IApplicationContext context)
        {
            return context.GetService<IMessageChannel>(ERROR_CHANNEL_BEAN_NAME);
        }

        public static IMessageChannel GetNullChannel(IApplicationContext context)
        {
            return context.GetService<IMessageChannel>(NULL_CHANNEL_BEAN_NAME);
        }

        public static IEvaluationContext GetEvaluationContext(IApplicationContext context)
        {
            return context.GetService<IEvaluationContext>(INTEGRATION_EVALUATION_CONTEXT_BEAN_NAME);
        }

        public static IEvaluationContext GetSimpleEvaluationContext(IApplicationContext context)
        {
            return context.GetService<IEvaluationContext>(INTEGRATION_SIMPLE_EVALUATION_CONTEXT_BEAN_NAME);
        }

        public static IExpressionParser GetExpressionParser(IApplicationContext context)
        {
            var result = context.GetService<IExpressionParser>() ?? new SpelExpressionParser();

            return result;
        }
    }
}
