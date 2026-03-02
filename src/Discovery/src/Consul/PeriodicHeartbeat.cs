// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Extensions;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Steeltoe.Discovery.Consul;

internal sealed partial class PeriodicHeartbeat : IAsyncDisposable
{
    private readonly string _serviceId;
    private readonly PeriodicTimer _periodicTimer;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly IConsulClient _client;
    private readonly ILogger<PeriodicHeartbeat> _logger;
    private readonly Task _task;

    internal TimeSpan Interval { get; private set; }

    public PeriodicHeartbeat(string serviceId, TimeSpan interval, IConsulClient client, ILogger<PeriodicHeartbeat> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(logger);

        _serviceId = serviceId;
        Interval = interval;
        _periodicTimer = new PeriodicTimer(interval);
        _client = client;
        _logger = logger;
        _task = TimerLoopAsync();
    }

    private async Task TimerLoopAsync()
    {
        try
        {
            LogStartSendingHeartbeats(_serviceId, Interval);

            while (await _periodicTimer.WaitForNextTickAsync(_cancellationTokenSource.Token))
            {
                LogSendingHeartbeat(_serviceId);

                try
                {
                    await _client.Agent.PassTTL(_serviceId, "ttl", _cancellationTokenSource.Token);
                }
                catch (Exception exception) when (!exception.IsCancellation())
                {
                    LogFailedToSendHeartbeat(exception, _serviceId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            LogSendingHeartbeatsStopped(_serviceId);
        }
    }

    public void ChangeInterval(TimeSpan interval)
    {
        if (interval != Interval)
        {
            _periodicTimer.Period = interval;
            Interval = interval;
            LogHeartbeatIntervalChanged(_serviceId, interval);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cancellationTokenSource.CancelAsync();
        await _task;
        _cancellationTokenSource.Dispose();
        _task.Dispose();
        _periodicTimer.Dispose();
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Start sending periodic Consul heartbeats for '{ServiceId}' with interval {Interval}.")]
    private partial void LogStartSendingHeartbeats(string serviceId, TimeSpan interval);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Sending Consul heartbeat for '{ServiceId}'.")]
    private partial void LogSendingHeartbeat(string serviceId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send Consul heartbeat for '{ServiceId}'.")]
    private partial void LogFailedToSendHeartbeat(Exception exception, string serviceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Stopped sending periodic Consul heartbeats for '{ServiceId}'.")]
    private partial void LogSendingHeartbeatsStopped(string serviceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Periodic Consul heartbeat interval for '{ServiceId}' changed to {Interval}.")]
    private partial void LogHeartbeatIntervalChanged(string serviceId, TimeSpan interval);
}
