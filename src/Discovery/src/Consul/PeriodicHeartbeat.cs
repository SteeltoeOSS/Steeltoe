// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;

namespace Steeltoe.Discovery.Consul;

internal sealed class PeriodicHeartbeat : IAsyncDisposable
{
    private readonly string _serviceId;
    private readonly TimeSpan _interval;
    private readonly PeriodicTimer _periodicTimer;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly IConsulClient _client;
    private readonly ILogger<PeriodicHeartbeat> _logger;
    private readonly Task _task;

    public PeriodicHeartbeat(string serviceId, TimeSpan interval, IConsulClient client, ILogger<PeriodicHeartbeat> logger)
    {
        ArgumentGuard.NotNull(serviceId);
        ArgumentGuard.NotNull(client);
        ArgumentGuard.NotNull(logger);

        _serviceId = serviceId;
        _interval = interval;
        _periodicTimer = new PeriodicTimer(interval);
        _client = client;
        _logger = logger;
        _task = TimerLoopAsync();
    }

    private async Task TimerLoopAsync()
    {
        try
        {
            _logger.LogDebug("Start sending periodic Consul heartbeats for '{ServiceId}' with interval {Interval}.", _serviceId, _interval);

            while (await _periodicTimer.WaitForNextTickAsync(_cancellationTokenSource.Token))
            {
                _logger.LogDebug("Sending Consul heartbeat for '{ServiceId}'.", _serviceId);

                try
                {
                    await _client.Agent.PassTTL(_serviceId, "ttl", _cancellationTokenSource.Token);
                }
                catch (Exception exception) when (!exception.IsCancellation())
                {
                    _logger.LogError(exception, "Failed to send Consul heartbeat for '{ServiceId}'.", _serviceId);
                }
            }
        }
        catch (OperationCanceledException exception)
        {
            _logger.LogDebug(exception, "Stop sending periodic Consul heartbeats for '{ServiceId}'.", _serviceId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cancellationTokenSource.CancelAsync();
        await _task;
        _cancellationTokenSource.Dispose();
        _task.Dispose();
    }
}
