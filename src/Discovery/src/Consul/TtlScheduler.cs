// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Discovery.Consul.Configuration;

namespace Steeltoe.Discovery.Consul;

/// <summary>
/// Scheduler used to issue TTL (time-to-live) requests to the Consul server.
/// </summary>
public sealed class TtlScheduler : IAsyncDisposable
{
    private const string InstancePrefix = "service:";

    private readonly IOptionsMonitor<ConsulDiscoveryOptions> _optionsMonitor;
    private readonly IConsulClient _client;
    private readonly ILogger<TtlScheduler> _schedulerLogger;
    private readonly ILogger<PeriodicHeartbeat> _heartbeatLogger;
    private readonly IDisposable? _optionsChangeToken;
    private bool _isDisposed;

    internal ConcurrentDictionary<string, PeriodicHeartbeat> ServiceHeartbeats { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="TtlScheduler" /> class.
    /// </summary>
    /// <param name="optionsMonitor">
    /// Provides access to <see cref="ConsulDiscoveryOptions" />.
    /// </param>
    /// <param name="client">
    /// The Consul client.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public TtlScheduler(IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor, IConsulClient client, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _optionsMonitor = optionsMonitor;
        _client = client;
        _schedulerLogger = loggerFactory.CreateLogger<TtlScheduler>();
        _heartbeatLogger = loggerFactory.CreateLogger<PeriodicHeartbeat>();
        _optionsChangeToken = optionsMonitor.OnChange(HandleOptionsChanged);
    }

    private void HandleOptionsChanged(ConsulDiscoveryOptions options)
    {
        if (options is { InstanceId: not null, Heartbeat: not null })
        {
            AddOrUpdate(options.InstanceId, options.Heartbeat);
        }
    }

    /// <summary>
    /// Adds an instance to be checked.
    /// </summary>
    /// <param name="instanceId">
    /// The instance ID.
    /// </param>
    internal void Add(string instanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        ConsulHeartbeatOptions? heartbeatOptions = _optionsMonitor.CurrentValue.Heartbeat;

        if (heartbeatOptions != null)
        {
            AddOrUpdate(instanceId, heartbeatOptions);
        }
    }

    private void AddOrUpdate(string instanceId, ConsulHeartbeatOptions heartbeatOptions)
    {
        _schedulerLogger.LogDebug("Adding/updating instance '{InstanceId}'.", instanceId);

        TimeSpan interval = heartbeatOptions.ComputeHeartbeatInterval();
        string checkId = instanceId;

        if (!checkId.StartsWith(InstancePrefix, StringComparison.Ordinal))
        {
            checkId = $"{InstancePrefix}{checkId}";
        }

        ServiceHeartbeats.AddOrUpdate(instanceId, _ => new PeriodicHeartbeat(checkId, interval, _client, _heartbeatLogger), (_, periodicHeartbeat) =>
        {
            periodicHeartbeat.ChangeInterval(interval);
            return periodicHeartbeat;
        });
    }

    /// <summary>
    /// Removes an instance from checking.
    /// </summary>
    /// <param name="instanceId">
    /// The instance ID.
    /// </param>
    internal async Task RemoveAsync(string instanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        if (ServiceHeartbeats.TryRemove(instanceId, out PeriodicHeartbeat? heartbeat))
        {
            _schedulerLogger.LogDebug("Removing instance '{InstanceId}'.", instanceId);
            await heartbeat.DisposeAsync();
        }
    }

    /// <summary>
    /// Removes all heartbeats from this scheduler.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_isDisposed)
        {
            foreach (string instanceId in ServiceHeartbeats.Keys)
            {
                await RemoveAsync(instanceId);
            }

            _optionsChangeToken?.Dispose();
            _isDisposed = true;
        }
    }
}
