// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Steeltoe.Common;
using Steeltoe.Common.Util;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.AppInfo;

/// <summary>
/// Represents a collection of applications in Eureka server.
/// </summary>
public sealed class ApplicationInfoCollection
{
    private readonly object _addRemoveInstanceLock = new();

    internal ConcurrentDictionary<string, ApplicationInfo> ApplicationMap { get; } = new();
    internal ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> VipInstanceMap { get; } = new();
    internal ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> SecureVipInstanceMap { get; } = new();

    public IReadOnlyList<ApplicationInfo> RegisteredApplications => new List<ApplicationInfo>(ApplicationMap.Values);
    public string? AppsHashCode { get; internal set; }
    public long? Version { get; private set; }
    public bool ReturnUpInstancesOnly { get; set; }

    internal ApplicationInfoCollection()
        : this(Array.Empty<ApplicationInfo>())
    {
    }

    internal ApplicationInfoCollection(IList<ApplicationInfo> apps)
    {
        ArgumentGuard.NotNull(apps);
        ArgumentGuard.ElementsNotNull(apps);

        foreach (ApplicationInfo app in apps)
        {
            Add(app);
        }
    }

    internal ApplicationInfo? GetRegisteredApplication(string appName)
    {
        ArgumentGuard.NotNullOrWhiteSpace(appName);

        return ApplicationMap.GetValueOrDefault(appName.ToUpperInvariant());
    }

    internal IReadOnlyList<InstanceInfo> GetInstancesBySecureVipAddress(string secureVipAddress)
    {
        ArgumentGuard.NotNullOrWhiteSpace(secureVipAddress);

        return GetByVipAddress(secureVipAddress, SecureVipInstanceMap);
    }

    internal IReadOnlyList<InstanceInfo> GetInstancesByVipAddress(string vipAddress)
    {
        ArgumentGuard.NotNullOrWhiteSpace(vipAddress);

        return GetByVipAddress(vipAddress, VipInstanceMap);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, DebugSerializerOptions.Instance);
    }

    internal void Add(ApplicationInfo app)
    {
        ArgumentGuard.NotNull(app);

        ApplicationMap.AddOrUpdate(app.Name.ToUpperInvariant(), app, (_, _) => app);

        foreach (InstanceInfo instance in app.Instances)
        {
            AddInstanceToVip(instance);
        }
    }

    private void AddInstanceToVip(InstanceInfo instance)
    {
        foreach (string vipAddress in ExpandVipAddresses(instance.VipAddress))
        {
            AddInstanceToVip(instance, vipAddress, VipInstanceMap);
        }

        foreach (string secureVipAddress in ExpandVipAddresses(instance.SecureVipAddress))
        {
            AddInstanceToVip(instance, secureVipAddress, SecureVipInstanceMap);
        }
    }

    private void AddInstanceToVip(InstanceInfo instance, string address, ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> dictionary)
    {
        lock (_addRemoveInstanceLock)
        {
            string addressUpper = address.ToUpperInvariant();

            if (!dictionary.TryGetValue(addressUpper, out ConcurrentDictionary<string, InstanceInfo>? instances))
            {
                instances = new ConcurrentDictionary<string, InstanceInfo>();
                dictionary[addressUpper] = instances;
            }

            instances[instance.InstanceId] = instance;
        }
    }

    private static ICollection<string> ExpandVipAddresses(string? addresses)
    {
        if (string.IsNullOrWhiteSpace(addresses))
        {
            return [];
        }

        return addresses.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToArray();
    }

    internal void RemoveInstanceFromVip(InstanceInfo instance)
    {
        ArgumentGuard.NotNull(instance);

        foreach (string vipAddress in ExpandVipAddresses(instance.VipAddress))
        {
            RemoveInstanceFromVip(instance, vipAddress, VipInstanceMap);
        }

        foreach (string secureVipAddress in ExpandVipAddresses(instance.SecureVipAddress))
        {
            RemoveInstanceFromVip(instance, secureVipAddress, SecureVipInstanceMap);
        }
    }

    private void RemoveInstanceFromVip(InstanceInfo instance, string address,
        ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> dictionary)
    {
        lock (_addRemoveInstanceLock)
        {
            string addressUpper = address.ToUpperInvariant();

            if (dictionary.TryGetValue(addressUpper, out ConcurrentDictionary<string, InstanceInfo>? instances))
            {
                _ = instances.TryRemove(instance.InstanceId, out _);

                if (instances.Count == 0)
                {
                    _ = dictionary.TryRemove(addressUpper, out _);
                }
            }
        }
    }

    internal void UpdateFromDelta(ApplicationInfoCollection delta)
    {
        ArgumentGuard.NotNull(delta);

        foreach (ApplicationInfo app in delta.RegisteredApplications)
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
                        AddInstanceToVip(instance);
                        break;
                    case ActionType.Deleted:
                        existingApp.Remove(instance);
                        RemoveInstanceFromVip(instance);
                        break;
                }
            }
        }

        Version = delta.Version;
    }

    internal string ComputeHashCode()
    {
        var statusMap = new Dictionary<string, int>();

        foreach (ApplicationInfo app in RegisteredApplications)
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

    internal static ApplicationInfoCollection FromJson(JsonApplications? jsonApplications)
    {
        var apps = new ApplicationInfoCollection();

        if (jsonApplications != null)
        {
            apps.Version = jsonApplications.VersionDelta;
            apps.AppsHashCode = jsonApplications.AppsHashCode;

            if (jsonApplications.Applications != null)
            {
                foreach (JsonApplication? application in jsonApplications.Applications)
                {
                    ApplicationInfo? app = ApplicationInfo.FromJson(application);

                    if (app != null)
                    {
                        apps.Add(app);
                    }
                }
            }
        }

        return apps;
    }

    private IReadOnlyList<InstanceInfo> GetByVipAddress(string name, ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> dictionary)
    {
        var result = new List<InstanceInfo>();

        if (dictionary.TryGetValue(name.ToUpperInvariant(), out ConcurrentDictionary<string, InstanceInfo>? instances))
        {
            foreach (InstanceInfo instance in instances.Values.ToArray())
            {
                if ((ReturnUpInstancesOnly && instance.EffectiveStatus == InstanceStatus.Up) || !ReturnUpInstancesOnly)
                {
                    result.Add(instance);
                }
            }
        }

        return result;
    }
}
