// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Messaging.Handler.Invocation;

/// <summary>
/// Invokes the underlying method with argument values resolved from the current message.
/// </summary>
public interface IInvocableHandlerMethod
{
    object Handler { get; }

    MethodInfo Method { get; }

    /// <summary>
    /// Gets a value indicating whether the return type of the method is void.
    /// </summary>
    bool IsVoid { get; }

    /// <summary>
    /// Gets a value for message logging (TODO: Look to remove).
    /// </summary>
    string ShortLogMessage { get; }

    /// <summary>
    /// Invoke the underlying method after resolving its argument values in the context of the given message.
    /// </summary>
    /// <param name="requestMessage">
    /// the message being processed.
    /// </param>
    /// <param name="args">
    /// given arguments matched by type, not resolved.
    /// </param>
    /// <returns>
    /// the raw value returned from the invoked method.
    /// </returns>
    object Invoke(IMessage requestMessage, params object[] args);
}
