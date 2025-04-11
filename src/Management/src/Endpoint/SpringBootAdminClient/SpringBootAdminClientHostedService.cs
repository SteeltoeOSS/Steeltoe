// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient;

internal sealed class SpringBootAdminClientHostedService : IHostedLifecycleService, IAsyncDisposable
{
    private readonly SpringBootAdminRefreshRunner _springBootAdminRefreshRunner;
    private readonly TimeProvider _timeProvider;
    private readonly IOptionsMonitor<SpringBootAdminClientOptions> _optionsMonitor;
    private readonly ILoggerFactory _loggerFactory;

    private SpringBootAdminPeriodicRefresh? _refresh;

    public SpringBootAdminClientHostedService(SpringBootAdminRefreshRunner springBootAdminRefreshRunner, TimeProvider timeProvider,
        IOptionsMonitor<SpringBootAdminClientOptions> optionsMonitor, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(springBootAdminRefreshRunner);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _springBootAdminRefreshRunner = springBootAdminRefreshRunner;
        _timeProvider = timeProvider;
        _optionsMonitor = optionsMonitor;
        _loggerFactory = loggerFactory;
    }

    public Task StartingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StartedAsync(CancellationToken cancellationToken)
    {
        ILogger<SpringBootAdminPeriodicRefresh> logger = _loggerFactory.CreateLogger<SpringBootAdminPeriodicRefresh>();
        _refresh = new SpringBootAdminPeriodicRefresh(_optionsMonitor, _timeProvider, _springBootAdminRefreshRunner, logger);
        return Task.CompletedTask;
    }

    public async Task StoppingAsync(CancellationToken cancellationToken)
    {
        if (_refresh != null)
        {
            await _refresh.StopAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StoppedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_refresh != null)
        {
            await _refresh.DisposeAsync();
        }
    }
}
