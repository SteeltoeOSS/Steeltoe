// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Discovery.Consul.Discovery;

/// <summary>
/// Scheduler used to issue TTL requests to the Consul server.
/// </summary>
public sealed class TtlScheduler : IAsyncDisposable
{
    private const string InstancePrefix = "service:";

    private readonly IOptionsMonitor<ConsulDiscoveryOptions> _optionsMonitor;
    private readonly IConsulClient _client;
    private readonly ILogger<TtlScheduler> _schedulerLogger;
    private readonly ILogger<PeriodicHeartbeat> _heartbeatLogger;
    internal readonly ConcurrentDictionary<string, PeriodicHeartbeat> ServiceHeartbeats = new(StringComparer.OrdinalIgnoreCase);
    private bool _isDisposed;

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
        ArgumentGuard.NotNull(optionsMonitor);
        ArgumentGuard.NotNull(client);
        ArgumentGuard.NotNull(loggerFactory);

        _optionsMonitor = optionsMonitor;
        _client = client;
        _schedulerLogger = loggerFactory.CreateLogger<TtlScheduler>();
        _heartbeatLogger = loggerFactory.CreateLogger<PeriodicHeartbeat>();
    }

    /// <summary>
    /// Adds an instance to be checked.
    /// </summary>
    /// <param name="instanceId">
    /// The instance ID.
    /// </param>
    public void Add(string instanceId)
    {
        ArgumentGuard.NotNullOrWhiteSpace(instanceId);

        ConsulHeartbeatOptions? heartbeatOptions = _optionsMonitor.CurrentValue.Heartbeat;

        if (heartbeatOptions != null)
        {
            _schedulerLogger.LogDebug("Adding instance '{instanceId}'.", instanceId);

            TimeSpan interval = heartbeatOptions.ComputeHeartbeatInterval();
            string checkId = instanceId;

            if (!checkId.StartsWith(InstancePrefix, StringComparison.Ordinal))
            {
                checkId = $"{InstancePrefix}{checkId}";
            }

            // Not using AddOrUpdate, because .NET 6 lacks support for changing PeriodicTimer.Period (added in .NET 8).
            _ = ServiceHeartbeats.GetOrAdd(instanceId, _ => new PeriodicHeartbeat(checkId, interval, _client, _heartbeatLogger));
        }
    }

    /// <summary>
    /// Removes an instance from checking.
    /// </summary>
    /// <param name="instanceId">
    /// The instance ID.
    /// </param>
    public async Task RemoveAsync(string instanceId)
    {
        ArgumentGuard.NotNullOrWhiteSpace(instanceId);

        if (ServiceHeartbeats.TryRemove(instanceId, out PeriodicHeartbeat? heartbeat))
        {
            _schedulerLogger.LogDebug("Removing instance '{instanceId}'.", instanceId);
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

            _isDisposed = true;
        }
    }
}
