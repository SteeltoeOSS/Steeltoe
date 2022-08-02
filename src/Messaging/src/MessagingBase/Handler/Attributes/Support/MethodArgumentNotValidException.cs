// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Messaging.Handler.Invocation;

namespace Steeltoe.Messaging.Handler.Attributes.Support;

public class MethodArgumentNotValidException : MethodArgumentResolutionException
{
    public MethodArgumentNotValidException(IMessage message, ParameterInfo parameter)
        : base(message, parameter)
    {
    }

    public MethodArgumentNotValidException(IMessage message, ParameterInfo parameter, string description)
        : base(message, parameter, description)
    {
    }
}
