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

using Steeltoe.Common.Converter;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Attributes.Support
{
    public class DestinationVariableMethodArgumentResolver : AbstractNamedValueMethodArgumentResolver
    {
        public const string DESTINATION_TEMPLATE_VARIABLES_HEADER = nameof(DestinationVariableMethodArgumentResolver) + ".templateVariables";

        public DestinationVariableMethodArgumentResolver(IConversionService conversionService)
            : base(conversionService)
        {
        }

        public override bool SupportsParameter(ParameterInfo parameter)
        {
            return parameter.GetCustomAttribute<DestinationVariableAttribute>() != null;
        }

        protected override NamedValueInfo CreateNamedValueInfo(ParameterInfo parameter)
        {
            var annot = parameter.GetCustomAttribute<DestinationVariableAttribute>();
            if (annot == null)
            {
                throw new InvalidOperationException("No DestinationVariable annotation");
            }

            return new DestinationVariableNamedValueInfo(annot);
        }

        protected override object ResolveArgumentInternal(ParameterInfo parameter, IMessage message, string name)
        {
            var headers = message.Headers;
            headers.TryGetValue(DESTINATION_TEMPLATE_VARIABLES_HEADER, out var obj);
            var vars = obj as IDictionary<string, object>;
            object result = null;
            if (vars != null)
            {
                vars.TryGetValue(name, out result);
            }

            return result;
        }

        protected override void HandleMissingValue(string name, ParameterInfo parameter, IMessage message)
        {
            throw new MessageHandlingException(message, "Missing path template variable '" + name + "' " + "for method parameter type [" + parameter.ParameterType + "]");
        }

        protected class DestinationVariableNamedValueInfo : NamedValueInfo
        {
            public DestinationVariableNamedValueInfo(DestinationVariableAttribute annotation)
                : base(annotation.Name, true)
            {
            }
        }
    }
}
