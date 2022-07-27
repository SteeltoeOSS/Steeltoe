// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Messaging.Handler.Invocation;

public class MethodArgumentResolutionException : MessagingException
{
    public ParameterInfo Parameter { get; private set; }

    public MethodArgumentResolutionException(IMessage message, ParameterInfo parameter)
        : base(message, GetMethodParameterMessage(parameter))
    {
        Parameter = parameter;
    }

    public MethodArgumentResolutionException(IMessage message, ParameterInfo parameter, string description)
        : base(message, GetMethodParameterMessage(parameter) + ": " + description)
    {
        Parameter = parameter;
    }

    private static string GetMethodParameterMessage(ParameterInfo parameter)
    {
        return "Could not resolve method parameter at index " + parameter.Position + " in " + parameter.Member.ToString();
    }
}