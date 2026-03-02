// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Management.Endpoint.Actuators.HttpExchanges.Diagnostics;

internal sealed partial class DiagnosticsService : IHostedService
{
    private readonly ILogger<DiagnosticsService> _logger;
    private readonly DiagnosticsManager _observerManager;

    public DiagnosticsService(DiagnosticsManager observerManager, ILogger<DiagnosticsService> logger)
    {
        ArgumentNullException.ThrowIfNull(observerManager);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        _observerManager = observerManager;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        LogStartingDiagnosticsManager();
        _observerManager.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        LogStoppingDiagnosticsManager();
        _observerManager.Stop();
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "Starting diagnostics manager.")]
    private partial void LogStartingDiagnosticsManager();

    [LoggerMessage(Level = LogLevel.Trace, Message = "Stopping diagnostics manager.")]
    private partial void LogStoppingDiagnosticsManager();
}
