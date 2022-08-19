// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Messaging.Handler.Invocation;

public class MethodArgumentResolutionException : MessagingException
{
    public ParameterInfo Parameter { get; }

    public MethodArgumentResolutionException(IMessage failedMessage, ParameterInfo parameter)
        : base(failedMessage, GetMethodParameterMessage(parameter))
    {
        Parameter = parameter;
    }

    public MethodArgumentResolutionException(IMessage failedMessage, ParameterInfo parameter, string message)
        : base(failedMessage, $"{GetMethodParameterMessage(parameter)}: {message}")
    {
        Parameter = parameter;
    }

    private static string GetMethodParameterMessage(ParameterInfo parameter)
    {
        return $"Could not resolve method parameter at index {parameter.Position} in {parameter.Member}";
    }
}
