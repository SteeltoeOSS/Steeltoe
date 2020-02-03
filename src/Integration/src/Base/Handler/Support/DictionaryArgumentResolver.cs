// Copyright 2017 the original author or authors.
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

using Steeltoe.Common.Expression;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Messaging.Handler.Invocation;
using System.Collections;
using System.Reflection;

namespace Steeltoe.Integration.Handler.Support
{
    public class DictionaryArgumentResolver : AbstractExpressionEvaluator, IHandlerMethodArgumentResolver
    {
        public DictionaryArgumentResolver(IExpressionParser expressionParser, IEvaluationContext evaluationContext)
            : base(expressionParser, evaluationContext)
        {
        }

        public object ResolveArgument(ParameterInfo parameter, IMessage message)
        {
            var payload = message.Payload;
            if (parameter.GetCustomAttribute<HeadersAttribute>() == null && payload is IDictionary)
            {
                return payload;
            }
            else
            {
                return message.Headers;
            }
        }

        public bool SupportsParameter(ParameterInfo parameter)
        {
            return parameter.GetCustomAttribute<PayloadAttribute>() == null &&
                typeof(IDictionary).IsAssignableFrom(parameter.ParameterType);
        }
    }
}
