// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common;

/// <summary>
/// A runnable asynchronous task bundled with the assembly that can be executed on-demand.
/// </summary>
public interface IApplicationTask
{
    /// <summary>
    /// Executes this task asynchronously.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    Task RunAsync(CancellationToken cancellationToken);
}
