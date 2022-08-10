// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Diagnostics;

namespace Steeltoe.Management.Endpoint.Diagnostics;

public class DiagnosticServices : IHostedService
{
    private readonly IDiagnosticsManager _observerManager;
    private readonly ILogger<DiagnosticServices> _logger;

    public DiagnosticServices(IDiagnosticsManager observerManager, ILogger<DiagnosticServices> logger = null)
    {
        _observerManager = observerManager;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _observerManager.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _observerManager.Stop();
        return Task.CompletedTask;
    }
}
