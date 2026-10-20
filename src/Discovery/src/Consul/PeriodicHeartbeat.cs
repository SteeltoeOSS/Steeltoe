// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Extensions;
using Steeltoe.Discovery.Consul.Configuration;
using LockPrimitive =
#if NET10_0_OR_GREATER
    System.Threading.Lock
#else
    object
#endif
    ;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Steeltoe.Discovery.Consul;

/// <summary>
/// Periodically sends TTL (time-to-live) heartbeats for a registered service, and follows changes to the heartbeat interval in the options.
/// </summary>
/// <remarks>
/// Options change notifications arrive on an arbitrary thread, and can still arrive after the subscription has been disposed. All state that they touch
/// is therefore guarded by a single lock, which is never held across an await.
/// </remarks>
internal sealed partial class PeriodicHeartbeat : IAsyncDisposable
{
    private readonly LockPrimitive _lock = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly IConsulClient _client;
    private readonly ILogger<PeriodicHeartbeat> _logger;
    private readonly IDisposable? _optionsChangeSubscription;

    private TimeSpan _interval;
    private PeriodicTimer? _periodicTimer;
    private Task? _task;
    private bool _isDisposed;

    /// <summary>
    /// Gets the ID of the Consul check that heartbeats are sent for.
    /// </summary>
    internal string CheckId { get; }

    /// <summary>
    /// Gets the interval that is in effect: the period of the running timer, or the interval it will start with.
    /// </summary>
    internal TimeSpan Interval
    {
        get
        {
            lock (_lock)
            {
                return _periodicTimer?.Period ?? _interval;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PeriodicHeartbeat" /> class. No heartbeats are sent until <see cref="Start" /> is called.
    /// </summary>
    /// <param name="checkId">
    /// The ID of the Consul check to send heartbeats for.
    /// </param>
    /// <param name="interval">
    /// The initial interval between heartbeats.
    /// </param>
    /// <param name="optionsMonitor">
    /// Provides access to <see cref="ConsulDiscoveryOptions" />, to follow changes to the heartbeat interval.
    /// </param>
    /// <param name="client">
    /// The Consul client.
    /// </param>
    /// <param name="logger">
    /// Used for internal logging. Pass <see cref="NullLogger{T}.Instance" /> to disable logging.
    /// </param>
    public PeriodicHeartbeat(string checkId, TimeSpan interval, IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor, IConsulClient client,
        ILogger<PeriodicHeartbeat> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checkId);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(logger);

        CheckId = checkId;
        _interval = interval;
        _client = client;
        _logger = logger;

        // Subscribe first, so no change can be missed. Notifications that arrive before the lock below is released wait for it.
        _optionsChangeSubscription = optionsMonitor.OnChange(HandleOptionsChanged);

        lock (_lock)
        {
            // Not inside a notification callback, so reading the current value is safe. It covers changes since the caller took its snapshot.
            UpdateTimerInterval(optionsMonitor.CurrentValue.Heartbeat);
        }
    }

    private void HandleOptionsChanged(ConsulDiscoveryOptions options, string? name)
    {
        // Use the options passed to the callback, not the current value.
        ConsulHeartbeatOptions? heartbeatOptions = options.Heartbeat;

        lock (_lock)
        {
            UpdateTimerInterval(heartbeatOptions);
        }
    }

    private void UpdateTimerInterval(ConsulHeartbeatOptions? heartbeatOptions)
    {
        if (!_isDisposed && heartbeatOptions != null)
        {
            TimeSpan newInterval = heartbeatOptions.ComputeHeartbeatInterval();

            // Setting the period restarts the countdown, even when the value is unchanged. Every options reload raises this callback,
            // so without this check unrelated reloads would keep postponing the next heartbeat.
            if (newInterval != _interval)
            {
                _interval = newInterval;
                _periodicTimer?.Period = newInterval;

                LogHeartbeatIntervalChanged(CheckId, newInterval);
            }
        }
    }

    /// <summary>
    /// Starts sending heartbeats. Does nothing when already started or disposed.
    /// </summary>
    public void Start()
    {
        lock (_lock)
        {
            if (!_isDisposed && _task == null)
            {
                _periodicTimer = new PeriodicTimer(_interval);
                _task = TimerLoopAsync(_periodicTimer);
            }
        }
    }

    private async Task TimerLoopAsync(PeriodicTimer periodicTimer)
    {
        try
        {
            LogStartSendingHeartbeats(CheckId, _interval);

            while (await periodicTimer.WaitForNextTickAsync(_cancellationTokenSource.Token))
            {
                LogSendingHeartbeat(CheckId);

                try
                {
                    await _client.Agent.PassTTL(CheckId, "ttl", _cancellationTokenSource.Token);
                }
                catch (Exception exception) when (!exception.IsCancellation())
                {
                    LogFailedToSendHeartbeat(exception, CheckId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            LogSendingHeartbeatsStopped(CheckId);
        }
    }

    /// <summary>
    /// Stops sending heartbeats and stops following options changes. Safe to call multiple times.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        bool dispose;

        lock (_lock)
        {
            dispose = !_isDisposed;

            // From here on, change notifications (including ones already in flight) no longer touch the timer, and Start() no longer
            // creates one, so _task and _periodicTimer are final and can be used below without holding the lock (which cannot span an await).
            _isDisposed = true;
        }

        if (dispose)
        {
            _optionsChangeSubscription?.Dispose();
            await _cancellationTokenSource.CancelAsync();

            if (_task != null)
            {
                await _task;
            }

            _periodicTimer?.Dispose();
            _cancellationTokenSource.Dispose();
        }
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
