﻿// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
            ISmartMessageConverter compositeMessageConverter,
            IConversionService conversionService,
            IExpressionParser expressionParser,
            IEvaluationContext evaluationContext)
        {
            MessageConverter = compositeMessageConverter;
            var resolvers = new List<IHandlerMethodArgumentResolver>();
            resolvers.Add(new SmartPayloadArgumentResolver(compositeMessageConverter));
            resolvers.Add(new SmartMessageMethodArgumentResolver(compositeMessageConverter));
            resolvers.Add(new HeaderMethodArgumentResolver(conversionService));
            resolvers.Add(new HeadersMethodArgumentResolver());

            resolvers.Add(new NullAwarePayloadArgumentResolver(compositeMessageConverter));
            resolvers.Add(new PayloadExpressionArgumentResolver(expressionParser, evaluationContext));

            resolvers.Add(new PayloadsArgumentResolver(expressionParser, evaluationContext));

            resolvers.Add(new DictionaryArgumentResolver(expressionParser, evaluationContext));

            SetArgumentResolvers(resolvers);

            AfterPropertiesSet();
        }
    }
}
