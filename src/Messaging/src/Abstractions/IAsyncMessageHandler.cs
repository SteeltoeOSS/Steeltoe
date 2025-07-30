// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging;

/// <summary>
/// Simple contract for handling a Message
/// </summary>
[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public interface IAsyncMessageHandler
{
    /// <summary>
    /// Handle the given method
    /// </summary>
    /// <param name="message">the message to process</param>
    /// <param name="cancellationToken">token used to signal cancelation</param>
    /// <returns>a task to signal completion</returns>
    Task HandleMessage(IMessage message, CancellationToken cancellationToken = default);
}