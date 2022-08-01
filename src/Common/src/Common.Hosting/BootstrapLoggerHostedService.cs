// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.Hosting;

public class BootstrapLoggerHostedService : IHostedService
{
    private readonly ILoggerFactory _loggerFactory;

    public BootstrapLoggerHostedService(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Logging.BootstrapLoggerFactory.Instance.Update(_loggerFactory);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
