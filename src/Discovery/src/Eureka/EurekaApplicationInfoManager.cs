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
/// Provides access to the Eureka instance that represents the currently running application.
/// </summary>
public sealed class EurekaApplicationInfoManager : IDisposable
{
    private readonly IOptionsMonitor<EurekaClientOptions> _clientOptionsMonitor;
    private readonly IOptionsMonitor<EurekaInstanceOptions> _instanceOptionsMonitor;
    private readonly IDisposable? _instanceOptionsChangeToken;
    private readonly ILogger<EurekaApplicationInfoManager> _logger;
    private readonly object _instanceWriteLock = new();

    // Readers must never be blocked, as it may delay the periodic heartbeat.
    // Updates from user code must be synchronized with configuration changes.
    // After update, the readonly snapshot is replaced. Volatile prevents reading stale data.
    // Once metadata has been set from user code, it overrules what's in configuration.
    private volatile InstanceInfo _instance;
    private IReadOnlyDictionary<string, string?>? _explicitMetadata;

    /// <summary>
    /// Gets the instance that represents the currently running application.
    /// </summary>
    public InstanceInfo Instance => _instance;

    internal event EventHandler<InstanceChangedEventArgs>? InstanceChanged;

    public EurekaApplicationInfoManager(IOptionsMonitor<EurekaClientOptions> clientOptionsMonitor,
        IOptionsMonitor<EurekaInstanceOptions> instanceOptionsMonitor, ILogger<EurekaApplicationInfoManager> logger)
    {
        ArgumentGuard.NotNull(clientOptionsMonitor);
        ArgumentGuard.NotNull(instanceOptionsMonitor);
        ArgumentGuard.NotNull(logger);

        if (!clientOptionsMonitor.CurrentValue.Enabled)
        {
            _instance = InstanceInfo.Disabled;
            _instanceOptionsChangeToken = null;
        }
        else
        {
            _instance = InstanceInfo.FromConfiguration(instanceOptionsMonitor.CurrentValue);
            _instanceOptionsChangeToken = instanceOptionsMonitor.OnChange(HandleInstanceOptionsChanged);
        }

        _clientOptionsMonitor = clientOptionsMonitor;
        _logger = logger;
        _instanceOptionsMonitor = instanceOptionsMonitor;
    }

    private void HandleInstanceOptionsChanged(EurekaInstanceOptions instanceOptions)
    {
        _logger.LogDebug("Responding to changed configuration.");

        try
        {
            InnerUpdateInstance(instanceOptions, true);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to update Eureka instance from changed configuration.");
        }
    }

    /// <summary>
    /// Atomically updates <see cref="Instance" /> by refreshing from configuration and applying the requested changes.
    /// </summary>
    /// <param name="newStatus">
    /// The status to assign, or <c>null</c> to preserve the current status.
    /// </param>
    /// <param name="newOverriddenStatus">
    /// The overridden status to assign, or <c>null</c> to preserve the current overridden status.
    /// </param>
    /// <param name="newMetadata">
    /// The metadata to assign, or <c>null</c> to preserve the current metadata. Once metadata has been assigned from code, future metadata changes from
    /// configuration are ignored.
    /// </param>
    public void UpdateInstance(InstanceStatus? newStatus, InstanceStatus? newOverriddenStatus, IReadOnlyDictionary<string, string?>? newMetadata)
    {
        if (_clientOptionsMonitor.CurrentValue.Enabled)
        {
            // Execute even when all parameters are null, so it applies updates from configuration only.
            InnerUpdateInstance(_instanceOptionsMonitor.CurrentValue, true, newStatus, newOverriddenStatus, newMetadata);
        }
    }

    private void InnerUpdateInstance(EurekaInstanceOptions newInstanceOptions, bool raiseChangeEvent, InstanceStatus? newStatus = null,
        InstanceStatus? newOverriddenStatus = null, IReadOnlyDictionary<string, string?>? newMetadata = null)
    {
        // This method takes all writable instance parameters, to avoid sending a partially-updated (inconsistent) snapshot to Eureka.

        InstanceChangedEventArgs? eventArgs = null;

        lock (_instanceWriteLock)
        {
            InstanceInfo previousInstance = _instance;
            InstanceInfo newInstance;

            try
            {
                newInstance = MergeInstanceWithConfiguration(newInstanceOptions, previousInstance);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to adapt to configuration changes. Discarding updated configuration.");
                newInstance = previousInstance;
            }

            // Status in configuration is the initial startup status. New or previous instance status always overrules it.
            newInstance.ReplaceStatus(newStatus ?? previousInstance.Status);

            if (newOverriddenStatus != null)
            {
                newInstance.ReplaceOverriddenStatus(newOverriddenStatus);
            }

            if (newMetadata != null)
            {
                newInstance.ReplaceMetadata(newMetadata);
                _explicitMetadata = newMetadata;
            }

            newInstance.DetectChanges(previousInstance);

            if (newInstance.IsDirty)
            {
                _logger.LogDebug("Instance has changed.");
                _instance = newInstance;
                eventArgs = new InstanceChangedEventArgs(newInstance, previousInstance);
            }
            else
            {
                _logger.LogDebug("Instance has not changed.");
            }
        }

        if (raiseChangeEvent && eventArgs != null)
        {
            InstanceChanged?.Invoke(this, eventArgs);
        }
    }

    private InstanceInfo MergeInstanceWithConfiguration(EurekaInstanceOptions instanceOptions, InstanceInfo previousInstance)
    {
        if (instanceOptions.InstanceId != previousInstance.InstanceId)
        {
            // A change of InstanceId would require unregister, then re-register.
            _logger.LogWarning("Discarding change of InstanceId, which is not supported.");
            instanceOptions.InstanceId = previousInstance.InstanceId;
        }

        if (!string.Equals(instanceOptions.AppName, previousInstance.AppName, StringComparison.OrdinalIgnoreCase))
        {
            // A change of AppName would require unregister, then re-register.
            _logger.LogWarning("Discarding change of AppName, which is not supported.");
            instanceOptions.AppName = previousInstance.AppName;
        }

        InstanceInfo newInstance = InstanceInfo.FromConfiguration(instanceOptions);

        newInstance.ReplaceStatus(previousInstance.Status);
        newInstance.ReplaceOverriddenStatus(previousInstance.OverriddenStatus);

        if (_explicitMetadata != null)
        {
            newInstance.ReplaceMetadata(_explicitMetadata);
        }

        return newInstance;
    }

    internal void UpdateStatusWithoutRaisingEvent(InstanceStatus newStatus)
    {
        InnerUpdateInstance(_instanceOptionsMonitor.CurrentValue, false, newStatus);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _instanceOptionsChangeToken?.Dispose();
    }
}
