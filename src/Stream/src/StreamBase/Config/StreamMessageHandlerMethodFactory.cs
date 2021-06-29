// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Converter;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Integration.Handler.Support;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Messaging.Handler.Invocation;
using System.Collections.Generic;

namespace Steeltoe.Stream.Config
{
    public class StreamMessageHandlerMethodFactory : DefaultMessageHandlerMethodFactory
    {
        public StreamMessageHandlerMethodFactory(
            IApplicationContext applicationContext,
            ISmartMessageConverter compositeMessageConverter,
            IConversionService conversionService)
            : base(conversionService, compositeMessageConverter)
        {
            MessageConverter = compositeMessageConverter;
            var resolvers = new List<IHandlerMethodArgumentResolver>
            {
                new SmartPayloadArgumentResolver(compositeMessageConverter),
                new SmartMessageMethodArgumentResolver(compositeMessageConverter),
                new HeaderMethodArgumentResolver(conversionService),
                new HeadersMethodArgumentResolver(),

                new NullAwarePayloadArgumentResolver(compositeMessageConverter),
                new PayloadExpressionArgumentResolver(applicationContext),

                new PayloadsArgumentResolver(applicationContext),

                new DictionaryArgumentResolver(applicationContext)
            };

            SetArgumentResolvers(resolvers);
        }
    }
}
