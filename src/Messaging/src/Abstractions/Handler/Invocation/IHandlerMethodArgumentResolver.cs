// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Messaging.Handler.Invocation;

/// <summary>
/// Strategy interface for resolving method parameters into argument values in the context of a given request.
/// </summary>
public interface IHandlerMethodArgumentResolver
{
    /// <summary>
    /// Determine whether the given method parameter is supported by this resolver.
    /// </summary>
    /// <param name="parameter">the parameter info to consideer</param>
    /// <returns>true if it is supported</returns>
    bool SupportsParameter(ParameterInfo parameter);

    /// <summary>
    /// Resolves a method parameter into an argument value from a given message.
    /// </summary>
    /// <param name="parameter">the parameter info to consideer</param>
    /// <param name="message">the message</param>
    /// <returns>the resolved argument value</returns>
    object ResolveArgument(ParameterInfo parameter, IMessage message);
}