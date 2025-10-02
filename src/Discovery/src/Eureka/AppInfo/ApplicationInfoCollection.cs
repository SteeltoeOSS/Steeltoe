// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using Steeltoe.Common;
using Steeltoe.Common.CasingConventions;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.AppInfo;

/// <summary>
/// Represents a collection of applications in Eureka server.
/// </summary>
public sealed class ApplicationInfoCollection : IReadOnlyCollection<ApplicationInfo>
{
    internal ConcurrentDictionary<string, ApplicationInfo> ApplicationMap { get; } = new();
    internal ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> VipInstanceMap { get; } = new();

    public string? AppsHashCode { get; internal set; }
    public long? Version { get; private set; }
    public bool ReturnUpInstancesOnly { get; internal set; }
    public int Count => ApplicationMap.Count;

    internal ApplicationInfoCollection()
        : this(Array.Empty<ApplicationInfo>())
    {
    }

    internal ApplicationInfoCollection(IList<ApplicationInfo> apps)
    {
        ArgumentNullException.ThrowIfNull(apps);
        ArgumentGuard.ElementsNotNull(apps);

        foreach (ApplicationInfo app in apps)
        {
            Add(app);
        }
    }

    internal ApplicationInfo? GetRegisteredApplication(string appName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appName);

        return ApplicationMap.GetValueOrDefault(appName.ToUpperInvariant());
    }

    internal ReadOnlyCollection<InstanceInfo> GetInstancesByVipAddress(string vipAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vipAddress);

        List<InstanceInfo> result = [];
        string addressUpper = vipAddress.ToUpperInvariant();

        if (VipInstanceMap.TryGetValue(addressUpper, out ConcurrentDictionary<string, InstanceInfo>? instancesById))
        {
            foreach (InstanceInfo instance in instancesById.Values)
            {
                if ((ReturnUpInstancesOnly && instance.EffectiveStatus == InstanceStatus.Up) || !ReturnUpInstancesOnly)
                {
                    result.Add(instance);
                }
            }
        }

        return result.AsReadOnly();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, DebugSerializerOptions.Instance);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<ApplicationInfo> GetEnumerator()
    {
        foreach (ApplicationInfo app in ApplicationMap.Values.ToArray())
        {
            yield return app;
        }
    }

    internal void Add(ApplicationInfo app)
    {
        ArgumentNullException.ThrowIfNull(app);

        ApplicationMap.AddOrUpdate(app.Name.ToUpperInvariant(), app, (_, _) => app);

        foreach (InstanceInfo instance in app.Instances)
        {
            AddToVipInstanceMap(instance);
        }
    }

    private void AddToVipInstanceMap(InstanceInfo instance)
    {
        foreach (string vipAddress in ExpandVipAddresses(instance))
        {
            string addressUpper = vipAddress.ToUpperInvariant();

            ConcurrentDictionary<string, InstanceInfo> instancesById = VipInstanceMap.GetOrAdd(addressUpper, new ConcurrentDictionary<string, InstanceInfo>());
            instancesById.AddOrUpdate(instance.InstanceId, _ => instance, (_, _) => instance);
        }
    }

    private static HashSet<string> ExpandVipAddresses(InstanceInfo instance)
    {
        HashSet<string> addresses = [];

        if (instance.SecureVipAddress != null)
        {
            string[] secureAddresses = instance.SecureVipAddress.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            addresses.UnionWith(secureAddresses);
        }

        if (instance.VipAddress != null)
        {
            string[] vipAddresses = instance.VipAddress.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            addresses.UnionWith(vipAddresses);
        }

        return addresses;
    }

    internal void RemoveFromVipInstanceMap(InstanceInfo instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        foreach (string vipAddress in ExpandVipAddresses(instance))
        {
            string addressUpper = vipAddress.ToUpperInvariant();

            if (VipInstanceMap.TryGetValue(addressUpper, out ConcurrentDictionary<string, InstanceInfo>? instancesById))
            {
                instancesById.TryRemove(instance.InstanceId, out _);
            }
        }
    }

    internal void UpdateFromDelta(ApplicationInfoCollection delta)
    {
        ArgumentNullException.ThrowIfNull(delta);

        foreach (ApplicationInfo app in delta)
        {
            foreach (InstanceInfo instance in app.Instances)
            {
                ApplicationInfo? existingApp = GetRegisteredApplication(instance.AppName);

                if (existingApp == null)
                {
                    Add(app);
                    existingApp = GetRegisteredApplication(instance.AppName)!;
                }

                switch (instance.ActionType)
                {
                    case ActionType.Added:
                    case ActionType.Modified:
                        existingApp.Add(instance);
                        AddToVipInstanceMap(instance);
                        break;
                    case ActionType.Deleted:
                        existingApp.Remove(instance);
                        RemoveFromVipInstanceMap(instance);
                        break;
                }
            }
        }

        Version = delta.Version;
    }

    internal string ComputeHashCode()
    {
        var statusMap = new Dictionary<string, int>();

        foreach (ApplicationInfo app in this)
        {
            foreach (InstanceInfo instance in app.Instances)
            {
                string instanceStatus = (instance.Status ?? InstanceStatus.Unknown).ToSnakeCaseString(SnakeCaseStyle.AllCaps);

                if (!statusMap.TryGetValue(instanceStatus, out int count))
                {
                    statusMap.Add(instanceStatus, 1);
                }
                else
                {
                    statusMap[instanceStatus] = count + 1;
                }
            }
        }

        var hashcodeBuilder = new StringBuilder();

        foreach (KeyValuePair<string, int> pair in statusMap.OrderBy(pair => pair.Key))
        {
            hashcodeBuilder.Append($"{pair.Key}_{pair.Value}_");
        }

        return hashcodeBuilder.ToString();
    }

    internal static ApplicationInfoCollection FromJson(JsonApplications? jsonApplications, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        var apps = new ApplicationInfoCollection();

        if (jsonApplications != null)
        {
            apps.Version = jsonApplications.VersionDelta;
            apps.AppsHashCode = jsonApplications.AppsHashCode;

            if (jsonApplications.Applications != null)
            {
                foreach (JsonApplication? application in jsonApplications.Applications)
                {
                    ApplicationInfo? app = ApplicationInfo.FromJson(application, timeProvider);

                    if (app != null)
                    {
                        apps.Add(app);
                    }
                }
            }
        }

        return apps;
    }
}
