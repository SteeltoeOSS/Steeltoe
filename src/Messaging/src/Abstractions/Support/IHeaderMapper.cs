// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.Support;

/// <summary>
/// Generic strategy interface for mapping MessageHeaders to and from other types of objects.
/// </summary>
/// <typeparam name="T">
/// type of the instance to and from which headers will be mapped.
/// </typeparam>
public interface IHeaderMapper<in T>
{
    /// <summary>
    /// Map from the given MessageHeaders to the specified target message.
    /// </summary>
    /// <param name="headers">
    /// the incoming message headers.
    /// </param>
    /// <param name="target">
    /// the native target message.
    /// </param>
    void FromHeaders(IMessageHeaders headers, T target);

    /// <summary>
    /// Map from the given target message to abstracted MessageHeaders.
    /// </summary>
    /// <param name="source">
    /// the native target message.
    /// </param>
    /// <returns>
    /// the mapped message headers.
    /// </returns>
    IMessageHeaders ToHeaders(T source);
}
