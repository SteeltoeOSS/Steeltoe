// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
