// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Management.Diagnostics;

namespace Steeltoe.Management.Endpoint.Diagnostics;

public class DiagnosticServices : IHostedService
{
    private readonly ILogger<DiagnosticServices> _logger;
    private readonly IDiagnosticsManager _observerManager;

    public DiagnosticServices(IDiagnosticsManager observerManager, ILogger<DiagnosticServices> logger)
    {
        ArgumentGuard.NotNull(logger);

        _logger = logger;
        _observerManager = observerManager;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogTrace("Starting Diagnostics manager");
        _observerManager.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogTrace("Stopping Diagnostics manager");
        _observerManager.Stop();
        return Task.CompletedTask;
    }
}
