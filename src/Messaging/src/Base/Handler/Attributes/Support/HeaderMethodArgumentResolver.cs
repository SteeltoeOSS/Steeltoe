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
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Attributes.Support
{
    public class HeaderMethodArgumentResolver : AbstractNamedValueMethodArgumentResolver
    {
        public HeaderMethodArgumentResolver(IConversionService conversionService)
            : base(conversionService)
        {
        }

        public override bool SupportsParameter(ParameterInfo parameter)
        {
            return parameter.GetCustomAttribute<HeaderAttribute>() != null;
        }

        protected override NamedValueInfo CreateNamedValueInfo(ParameterInfo parameter)
        {
            var annot = parameter.GetCustomAttribute<HeaderAttribute>();
            if (annot == null)
            {
                throw new InvalidOperationException("No Header annotation");
            }

            return new HeaderNamedValueInfo(annot);
        }

        protected override object ResolveArgumentInternal(ParameterInfo parameter, IMessage message, string name)
        {
            message.Headers.TryGetValue(name, out var headerValue);
            var nativeHeaderValue = GetNativeHeaderValue(message, name);

            if (headerValue != null && nativeHeaderValue != null)
            {
                // if (logger.isDebugEnabled())
                // {
                //    logger.debug("A value was found for '" + name + "', in both the top level header map " +
                //            "and also in the nested map for native headers. Using the value from top level map. " +
                //            "Use 'nativeHeader.myHeader' to resolve the native header.");
                // }
            }

            return headerValue ?? nativeHeaderValue;
        }

        protected override void HandleMissingValue(string headerName, ParameterInfo parameter, IMessage message)
        {
            throw new MessageHandlingException(message, "Missing header '" + headerName + "' for method parameter type [" + parameter.ParameterType + "]");
        }

        private object GetNativeHeaderValue(IMessage message, string name)
        {
            var nativeHeaders = GetNativeHeaders(message);
            if (name.StartsWith("nativeHeaders."))
            {
                name = name.Substring("nativeHeaders.".Length);
            }

            if (nativeHeaders == null || !nativeHeaders.ContainsKey(name))
            {
                return null;
            }

            nativeHeaders.TryGetValue(name, out var nativeHeaderValues);
            if (nativeHeaderValues.Count == 1)
            {
                return nativeHeaderValues[0];
            }

            return nativeHeaderValues;
        }

        private IDictionary<string, List<string>> GetNativeHeaders(IMessage message)
        {
            message.Headers.TryGetValue(NativeMessageHeaderAccessor.NATIVE_HEADERS, out var result);
            return (IDictionary<string, List<string>>)result;
        }

        private sealed class HeaderNamedValueInfo : NamedValueInfo
        {
            public HeaderNamedValueInfo(HeaderAttribute annotation)
            : base(annotation.Name, annotation.Required, annotation.DefaultValue)
            {
            }
        }
    }
}
