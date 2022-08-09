// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Text;
using Steeltoe.Common;
using Steeltoe.Common.Util;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.AppInfo;

public class Applications
{
    private readonly object _addRemoveInstanceLock = new();

    internal ConcurrentDictionary<string, Application> ApplicationMap { get; } = new();

    internal ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> VirtualHostInstanceMap { get; } = new();

    internal ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> SecureVirtualHostInstanceMap { get; } = new();

    public string AppsHashCode { get; internal set; }

    public long Version { get; internal set; }

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

    internal void AddInstances(Application app)
    {
        foreach (InstanceInfo inst in app.Instances)
        {
            AddInstanceToVip(inst);
        }
    }

    internal void AddInstanceToVip(InstanceInfo inst)
    {
        if (!string.IsNullOrEmpty(inst.VipAddress))
        {
            AddInstanceToVip(inst.VipAddress, inst, VirtualHostInstanceMap);
        }

        if (!string.IsNullOrEmpty(inst.SecureVipAddress))
        {
            AddInstanceToVip(inst.SecureVipAddress, inst, SecureVirtualHostInstanceMap);
        }
    }

    internal void AddInstanceToVip(string address, InstanceInfo info, ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> dict)
    {
        if (info.InstanceId == null)
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

            instances[info.InstanceId] = info;
        }
    }

    internal void RemoveInstanceFromVip(InstanceInfo inst)
    {
        if (!string.IsNullOrEmpty(inst.VipAddress))
        {
            RemoveInstanceFromVip(inst.VipAddress, inst, VirtualHostInstanceMap);
        }

        if (!string.IsNullOrEmpty(inst.SecureVipAddress))
        {
            RemoveInstanceFromVip(inst.SecureVipAddress, inst, SecureVirtualHostInstanceMap);
        }
    }

    internal void RemoveInstanceFromVip(string address, InstanceInfo info, ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> dict)
    {
        lock (_addRemoveInstanceLock)
        {
            string addressUpper = address.ToUpperInvariant();
            dict.TryGetValue(addressUpper, out ConcurrentDictionary<string, InstanceInfo> instances);

            if (instances != null)
            {
                instances.TryRemove(info.InstanceId, out _);

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
                        // logger.debug("Added instance {} to the existing apps in region {}", instance.getId(), instanceRegion);
                        existingApp.Add(instance);
                        AddInstanceToVip(instance);
                        break;
                    case ActionType.Deleted:
                        // logger.debug("Deleted instance {} to the existing apps ", instance.getId());
                        existingApp.Remove(instance);
                        RemoveInstanceFromVip(instance);
                        break;
                }
            }
        }

        // logger.debug(The total number of instances fetched by the delta processor : {}", deltaCount);
        Version = delta.Version;
    }

    internal string ComputeHashCode()
    {
        var statusMap = new Dictionary<string, int>();

        foreach (Application app in GetRegisteredApplications())
        {
            foreach (InstanceInfo inst in app.Instances)
            {
                string instanceStatus = inst.Status.ToSnakeCaseString(SnakeCaseStyle.AllCaps);

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
                InstanceInfo inst = kvp.Value;

                if (ReturnUpInstancesOnly)
                {
                    if (inst.Status == InstanceStatus.Up)
                    {
                        result.Add(inst);
                    }
                }
                else
                {
                    result.Add(inst);
                }
            }
        }

        return result;
    }
}
