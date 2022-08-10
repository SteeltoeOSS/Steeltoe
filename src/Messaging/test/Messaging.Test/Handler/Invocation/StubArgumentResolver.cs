// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Messaging.Handler.Invocation.Test;

internal sealed class StubArgumentResolver : IHandlerMethodArgumentResolver
{
    private readonly Type _valueType;

    private readonly object _value;

    public List<ParameterInfo> ResolvedParameters { get; } = new();

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
        _valueType = valueType;
        _value = value;
    }

    public bool SupportsParameter(ParameterInfo parameter)
    {
        return parameter.ParameterType.IsAssignableFrom(_valueType);
    }

    public object ResolveArgument(ParameterInfo parameter, IMessage message)
    {
        ResolvedParameters.Add(parameter);
        return _value;
    }
}
