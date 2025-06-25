// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.Logging;

internal sealed class UpgradeBootstrapLoggerHostedService : IHostedService
{
    private readonly BootstrapLoggerFactory _bootstrapLoggerFactory;
    private readonly ILoggerFactory _replacementLoggerFactory;

    public UpgradeBootstrapLoggerHostedService(BootstrapLoggerFactory bootstrapLoggerFactory, ILoggerFactory replacementLoggerFactory)
    {
        ArgumentNullException.ThrowIfNull(bootstrapLoggerFactory);
        ArgumentNullException.ThrowIfNull(replacementLoggerFactory);

        _bootstrapLoggerFactory = bootstrapLoggerFactory;
        _replacementLoggerFactory = replacementLoggerFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _bootstrapLoggerFactory.Upgrade(_replacementLoggerFactory);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
