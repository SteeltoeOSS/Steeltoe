// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Concurrent;
using System.Text;
using Steeltoe.Common;
using Steeltoe.Common.Util;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.AppInfo;

public sealed class Applications
{
    private readonly object _addRemoveInstanceLock = new();

    internal ConcurrentDictionary<string, Application> ApplicationMap { get; } = new();
    internal ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> VirtualHostInstanceMap { get; } = new();
    internal ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> SecureVirtualHostInstanceMap { get; } = new();

    public string? AppsHashCode { get; internal set; }
    public long? Version { get; private set; }
    public bool ReturnUpInstancesOnly { get; set; }

    internal Applications()
        : this(Array.Empty<Application>())
    {
    }

    internal Applications(IList<Application> apps)
    {
        ArgumentGuard.NotNull(apps);
        ArgumentGuard.ElementsNotNull(apps);

        foreach (Application app in apps)
        {
            Add(app);
        }
    }

    public IList<Application> GetRegisteredApplications()
    {
        return ApplicationMap.Values.ToList();
    }

    public Application? GetRegisteredApplication(string appName)
    {
        ArgumentGuard.NotNullOrEmpty(appName);

        return ApplicationMap.GetValueOrDefault(appName.ToUpperInvariant());
    }

    public IList<InstanceInfo> GetInstancesBySecureVirtualHostName(string secureVirtualHostName)
    {
        ArgumentGuard.NotNullOrEmpty(secureVirtualHostName);

        return GetByVirtualHostName(secureVirtualHostName, SecureVirtualHostInstanceMap);
    }

    public IList<InstanceInfo> GetInstancesByVirtualHostName(string virtualHostName)
    {
        ArgumentGuard.NotNullOrEmpty(virtualHostName);

        return GetByVirtualHostName(virtualHostName, VirtualHostInstanceMap);
    }

    public override string ToString()
    {
        return $"Applications[{string.Join(',', ApplicationMap.Select(pair => pair.Value.ToString()))}]";
    }

    internal void Add(Application app)
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
        if (!string.IsNullOrEmpty(instance.VipAddress))
        {
            AddInstanceToVip(instance.VipAddress, instance, VirtualHostInstanceMap);
        }

        if (!string.IsNullOrEmpty(instance.SecureVipAddress))
        {
            AddInstanceToVip(instance.SecureVipAddress, instance, SecureVirtualHostInstanceMap);
        }
    }

    private void AddInstanceToVip(string address, InstanceInfo instance, ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> dictionary)
    {
        if (instance.InstanceId == null)
        {
            return;
        }

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

    internal void RemoveInstanceFromVip(InstanceInfo instance)
    {
        ArgumentGuard.NotNull(instance);

        if (!string.IsNullOrEmpty(instance.VipAddress))
        {
            RemoveInstanceFromVip(instance.VipAddress, instance, VirtualHostInstanceMap);
        }

        if (!string.IsNullOrEmpty(instance.SecureVipAddress))
        {
            RemoveInstanceFromVip(instance.SecureVipAddress, instance, SecureVirtualHostInstanceMap);
        }
    }

    private void RemoveInstanceFromVip(string address, InstanceInfo instance,
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

    internal void UpdateFromDelta(Applications delta)
    {
        ArgumentGuard.NotNull(delta);

        foreach (Application app in delta.GetRegisteredApplications())
        {
            foreach (InstanceInfo instance in app.Instances)
            {
                Application? existingApp = GetRegisteredApplication(instance.AppName);

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

        foreach (Application app in GetRegisteredApplications())
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

    internal static Applications FromJsonApplications(JsonApplications? jsonApplications)
    {
        var apps = new Applications();

        if (jsonApplications != null)
        {
            apps.Version = jsonApplications.VersionDelta;
            apps.AppsHashCode = jsonApplications.AppsHashCode;

            if (jsonApplications.Applications != null)
            {
                foreach (JsonApplication? application in jsonApplications.Applications)
                {
                    var app = Application.FromJsonApplication(application);

                    if (app != null)
                    {
                        apps.Add(app);
                    }
                }
            }
        }

        return apps;
    }

    private IList<InstanceInfo> GetByVirtualHostName(string name, ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> dictionary)
    {
        var result = new List<InstanceInfo>();

        if (dictionary.TryGetValue(name.ToUpperInvariant(), out ConcurrentDictionary<string, InstanceInfo>? instances))
        {
            foreach (InstanceInfo instance in instances.Values.ToArray())
            {
                if ((ReturnUpInstancesOnly && instance.Status == InstanceStatus.Up) || !ReturnUpInstancesOnly)
                {
                    result.Add(instance);
                }
            }
        }

        return result;
    }
}
