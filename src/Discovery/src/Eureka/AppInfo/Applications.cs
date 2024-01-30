// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
    public string AppsHashCode { get; internal set; }
    public long Version { get; private set; }
    public bool ReturnUpInstancesOnly { get; set; }

    internal Applications()
    {
    }

    internal Applications(IList<Application> apps)
    {
        ArgumentGuard.NotNull(apps);

        foreach (Application app in apps)
        {
            Add(app);
        }
    }

    public IList<Application> GetRegisteredApplications()
    {
        return new List<Application>(ApplicationMap.Values);
    }

    public Application GetRegisteredApplication(string appName)
    {
        ArgumentGuard.NotNullOrEmpty(appName);

        ApplicationMap.TryGetValue(appName.ToUpperInvariant(), out Application result);
        return result;
    }

    public IList<InstanceInfo> GetInstancesBySecureVirtualHostName(string secureVirtualHostName)
    {
        ArgumentGuard.NotNullOrEmpty(secureVirtualHostName);

        return DoGetByVirtualHostName(secureVirtualHostName, SecureVirtualHostInstanceMap);
    }

    public IList<InstanceInfo> GetInstancesByVirtualHostName(string virtualHostName)
    {
        ArgumentGuard.NotNullOrEmpty(virtualHostName);

        return DoGetByVirtualHostName(virtualHostName, VirtualHostInstanceMap);
    }

    public override string ToString()
    {
        var sb = new StringBuilder("Applications[");

        foreach (KeyValuePair<string, Application> kvp in ApplicationMap)
        {
            sb.Append(kvp.Value);
        }

        sb.Append("]");

        return sb.ToString();
    }

    internal void Add(Application app)
    {
        ArgumentGuard.NotNull(app);

        ApplicationMap.AddOrUpdate(app.Name.ToUpperInvariant(), app, (_, _) => app);
        AddInstances(app);
    }

    private void AddInstances(Application app)
    {
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

    private void AddInstanceToVip(string address, InstanceInfo instance, ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> dict)
    {
        if (instance.InstanceId == null)
        {
            return;
        }

        lock (_addRemoveInstanceLock)
        {
            string addressUpper = address.ToUpperInvariant();
            dict.TryGetValue(addressUpper, out ConcurrentDictionary<string, InstanceInfo> instances);

            if (instances == null)
            {
                instances = dict[addressUpper] = new ConcurrentDictionary<string, InstanceInfo>();
            }

            instances[instance.InstanceId] = instance;
        }
    }

    internal void RemoveInstanceFromVip(InstanceInfo instance)
    {
        if (!string.IsNullOrEmpty(instance.VipAddress))
        {
            RemoveInstanceFromVip(instance.VipAddress, instance, VirtualHostInstanceMap);
        }

        if (!string.IsNullOrEmpty(instance.SecureVipAddress))
        {
            RemoveInstanceFromVip(instance.SecureVipAddress, instance, SecureVirtualHostInstanceMap);
        }
    }

    private void RemoveInstanceFromVip(string address, InstanceInfo instance, ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> dict)
    {
        lock (_addRemoveInstanceLock)
        {
            string addressUpper = address.ToUpperInvariant();
            dict.TryGetValue(addressUpper, out ConcurrentDictionary<string, InstanceInfo> instances);

            if (instances != null)
            {
                instances.TryRemove(instance.InstanceId, out _);

                if (instances.Count <= 0)
                {
                    _ = dict.TryRemove(addressUpper, out _);
                }
            }
        }
    }

    internal void UpdateFromDelta(Applications delta)
    {
        foreach (Application app in delta.GetRegisteredApplications())
        {
            foreach (InstanceInfo instance in app.Instances)
            {
                Application existingApp = GetRegisteredApplication(instance.AppName);

                if (existingApp == null)
                {
                    Add(app);
                    existingApp = GetRegisteredApplication(instance.AppName);
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
                string instanceStatus = instance.Status.ToSnakeCaseString(SnakeCaseStyle.AllCaps);

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

        IOrderedEnumerable<KeyValuePair<string, int>> query = statusMap.OrderBy(kvp => kvp.Key);
        var hashcodeBuilder = new StringBuilder();

        foreach (KeyValuePair<string, int> entry in query)
        {
            hashcodeBuilder.Append($"{entry.Key}_{entry.Value}_");
        }

        return hashcodeBuilder.ToString();
    }

    internal static Applications FromJsonApplications(JsonApplications applications)
    {
        var apps = new Applications();

        if (applications != null)
        {
            apps.Version = applications.VersionDelta;
            apps.AppsHashCode = applications.AppsHashCode;

            if (applications.Applications != null)
            {
                foreach (JsonApplication application in applications.Applications)
                {
                    var app = Application.FromJsonApplication(application);
                    apps.Add(app);
                }
            }
        }

        return apps;
    }

    private IList<InstanceInfo> DoGetByVirtualHostName(string name, ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> dict)
    {
        var result = new List<InstanceInfo>();

        if (dict.TryGetValue(name.ToUpperInvariant(), out ConcurrentDictionary<string, InstanceInfo> instances))
        {
            foreach (KeyValuePair<string, InstanceInfo> kvp in instances)
            {
                InstanceInfo instance = kvp.Value;

                if (ReturnUpInstancesOnly)
                {
                    if (instance.Status == InstanceStatus.Up)
                    {
                        result.Add(instance);
                    }
                }
                else
                {
                    result.Add(instance);
                }
            }
        }

        return result;
    }
}
