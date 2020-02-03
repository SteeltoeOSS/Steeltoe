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

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Invocation.Test
{
    internal class StubArgumentResolver : IHandlerMethodArgumentResolver
    {
        private readonly Type valueType;

        private readonly object value;

        private readonly List<ParameterInfo> resolvedParameters = new List<ParameterInfo>();

        public StubArgumentResolver(object value)
        : this(value.GetType(), value)
        {
        }

        public StubArgumentResolver(Type valueType)
        : this(valueType, null)
        {
        }

        public StubArgumentResolver(Type valueType, object value)
        {
            this.valueType = valueType;
            this.value = value;
        }

        public List<ParameterInfo> ResolvedParameters
        {
            get { return resolvedParameters; }
        }

        public bool SupportsParameter(ParameterInfo parameter)
        {
            return parameter.ParameterType.IsAssignableFrom(valueType);
        }

        public object ResolveArgument(ParameterInfo parameter, IMessage message)
        {
            resolvedParameters.Add(parameter);
            return value;
        }
    }
}
