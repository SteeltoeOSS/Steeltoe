// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;

namespace Steeltoe.Management.Tasks;

internal sealed class DelegatingTask : IApplicationTask
{
    private readonly Func<CancellationToken, Task> _asyncAction;

    public DelegatingTask(Func<CancellationToken, Task> asyncAction)
    {
        ArgumentNullException.ThrowIfNull(asyncAction);

        _asyncAction = asyncAction;
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _asyncAction(cancellationToken);
    }
}
