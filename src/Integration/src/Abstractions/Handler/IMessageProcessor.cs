// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Integration.Handler;

/// <summary>
/// This defines the lowest-level strategy of processing a Message and returning
/// some Object(or null). Implementations will be focused on generic concerns,
/// such as invoking a method, running a script, or evaluating an expression.
/// </summary>
public interface IMessageProcessor
{
    /// <summary>
    /// Process a message and return a value or null.
    /// </summary>
    /// <param name="message">message to process.</param>
    /// <returns>resulting object.</returns>
    object ProcessMessage(IMessage message);
}

/// <summary>
/// This defines the lowest-level strategy of processing a Message and returning
/// some Object(or null). Implementations will be focused on generic concerns,
/// such as invoking a method, running a script, or evaluating an expression.
/// </summary>
/// <typeparam name="T">the type of the processing result.</typeparam>
public interface IMessageProcessor<out T> : IMessageProcessor
{
    /// <summary>
    /// Process a message and return a value or null.
    /// </summary>
    /// <param name="message">message to process.</param>
    /// <returns>result after processing.</returns>
    new T ProcessMessage(IMessage message);
}
