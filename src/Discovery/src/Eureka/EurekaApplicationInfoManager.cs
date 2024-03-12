// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Steeltoe.Discovery.Eureka;

/// <summary>
/// Provides access to the Eureka instance that represents the currently running app.
/// </summary>
public sealed class EurekaApplicationInfoManager
{
    private readonly IOptionsMonitor<EurekaInstanceOptions> _instanceOptionsMonitor;
    private readonly ILogger<EurekaApplicationInfoManager> _logger;
    private readonly object _statusChangeLock = new();

    /// <summary>
    /// Gets the instance that represents the currently running app.
    /// </summary>
    public InstanceInfo Instance { get; }

    internal event EventHandler<InstanceStatusChangedEventArgs>? StatusChanged;

    public EurekaApplicationInfoManager(IOptionsMonitor<EurekaInstanceOptions> instanceOptionsMonitor, ILogger<EurekaApplicationInfoManager> logger)
    {
        ArgumentGuard.NotNull(instanceOptionsMonitor);
        ArgumentGuard.NotNull(logger);

        _instanceOptionsMonitor = instanceOptionsMonitor;
        _logger = logger;
        Instance = InstanceInfo.FromConfiguration(instanceOptionsMonitor.CurrentValue);
    }

    /// <summary>
    /// Updates the status of the instance that represents the currently running app.
    /// </summary>
    /// <param name="newStatus">
    /// The new status.
    /// </param>
    /// <remarks>
    /// Whereas changing <see cref="InstanceInfo.Status" /> marks the instance as dirty, so the change gets sent on the next heartbeat, this method sends the
    /// status immediately when eureka:client:shouldOnDemandUpdateStatusChange is set to <c>true</c> (the default).
    /// </remarks>
    public void UpdateStatus(InstanceStatus newStatus)
    {
        lock (_statusChangeLock)
        {
            InstanceStatus previousStatus = Instance.Status ?? InstanceStatus.Unknown;

            if (previousStatus != newStatus)
            {
                Instance.Status = newStatus;

                try
                {
                    StatusChanged?.Invoke(this, new InstanceStatusChangedEventArgs(previousStatus, newStatus, Instance.InstanceId));
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "StatusChanged event exception");
                }
            }
        }
    }

    internal IDisposable? SubscribeToConfigurationChange(Action<EurekaInstanceOptions> action)
    {
        return _instanceOptionsMonitor.OnChange(action);
    }
}
