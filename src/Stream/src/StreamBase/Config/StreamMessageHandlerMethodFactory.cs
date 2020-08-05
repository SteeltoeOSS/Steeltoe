// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Converter;
using Steeltoe.Common.Expression;
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
            var resolvers = new List<IHandlerMethodArgumentResolver>();
            resolvers.Add(new SmartPayloadArgumentResolver(compositeMessageConverter));
            resolvers.Add(new SmartMessageMethodArgumentResolver(compositeMessageConverter));
            resolvers.Add(new HeaderMethodArgumentResolver(conversionService));
            resolvers.Add(new HeadersMethodArgumentResolver());

            resolvers.Add(new NullAwarePayloadArgumentResolver(compositeMessageConverter));
            resolvers.Add(new PayloadExpressionArgumentResolver(applicationContext));

            resolvers.Add(new PayloadsArgumentResolver(applicationContext));

            resolvers.Add(new DictionaryArgumentResolver(applicationContext));

            SetArgumentResolvers(resolvers);
        }
    }
}
