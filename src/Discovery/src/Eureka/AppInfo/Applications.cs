// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Transport;
using System.Collections.Concurrent;
using System.Text;
using Steeltoe.Common.Util;

namespace Steeltoe.Discovery.Eureka.AppInfo;

public class Applications
{
    private readonly object _addRemoveInstanceLock = new ();

    public string AppsHashCode { get; internal set; }

    public long Version { get; internal set; }

    public bool ReturnUpInstancesOnly { get; set; }

    public IList<Application> GetRegisteredApplications()
    {
        return new List<Application>(ApplicationMap.Values);
    }

    public Application GetRegisteredApplication(string appName)
    {
        if (string.IsNullOrEmpty(appName))
        {
            throw new ArgumentException(nameof(appName));
        }

        ApplicationMap.TryGetValue(appName.ToUpperInvariant(), out var result);
        return result;
    }

    public IList<InstanceInfo> GetInstancesBySecureVirtualHostName(string secureVirtualHostName)
    {
        if (string.IsNullOrEmpty(secureVirtualHostName))
        {
            throw new ArgumentException(nameof(secureVirtualHostName));
        }

        return DoGetByVirtualHostName(secureVirtualHostName, SecureVirtualHostInstanceMap);
    }

    public IList<InstanceInfo> GetInstancesByVirtualHostName(string virtualHostName)
    {
        if (string.IsNullOrEmpty(virtualHostName))
        {
            throw new ArgumentException(nameof(virtualHostName));
        }

        return DoGetByVirtualHostName(virtualHostName, VirtualHostInstanceMap);
    }

    public override string ToString()
    {
        var sb = new StringBuilder("Applications[");
        foreach (var kvp in ApplicationMap)
        {
            sb.Append(kvp.Value);
        }

        sb.Append("]");

        return sb.ToString();
    }

    internal Applications()
    {
    }

    internal Applications(IList<Application> apps)
    {
        if (apps == null)
        {
            throw new ArgumentNullException(nameof(apps));
        }

        foreach (var app in apps)
        {
            Add(app);
        }
    }

    internal void Add(Application app)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        ApplicationMap.AddOrUpdate(app.Name.ToUpperInvariant(), app, (_, _) => app);
        AddInstances(app);
    }

    internal void AddInstances(Application app)
    {
        foreach (var inst in app.Instances)
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
            var addressUpper = address.ToUpperInvariant();
            dict.TryGetValue(addressUpper, out var instances);
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
            var addressUpper = address.ToUpperInvariant();
            dict.TryGetValue(addressUpper, out var instances);
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
        foreach (var app in delta.GetRegisteredApplications())
        {
            foreach (var instance in app.Instances)
            {
                var existingApp = GetRegisteredApplication(instance.AppName);
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
                    default:
                        // Log
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
        foreach (var app in GetRegisteredApplications())
        {
            foreach (var inst in app.Instances)
            {
                string instanceStatus = inst.Status.ToSnakeCaseString(SnakeCaseStyle.AllCaps);

                if (!statusMap.TryGetValue(instanceStatus, out var count))
                {
                    statusMap.Add(instanceStatus, 1);
                }
                else
                {
                    statusMap[instanceStatus] = count + 1;
                }
            }
        }

        var query = statusMap.OrderBy(kvp => kvp.Key);
        var hashcodeBuilder = new StringBuilder();
        foreach (var entry in query)
        {
            hashcodeBuilder.Append($"{entry.Key}_{entry.Value}_");
        }

        return hashcodeBuilder.ToString();
    }

    internal ConcurrentDictionary<string, Application> ApplicationMap { get; } = new ();

    internal ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> VirtualHostInstanceMap { get; } = new ();

    internal ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> SecureVirtualHostInstanceMap { get; } = new ();

    internal static Applications FromJsonApplications(JsonApplications applications)
    {
        var apps = new Applications();
        if (applications != null)
        {
            apps.Version = applications.VersionDelta;
            apps.AppsHashCode = applications.AppsHashCode;

            if (applications.Applications != null)
            {
                foreach (var application in applications.Applications)
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
        if (dict.TryGetValue(name.ToUpperInvariant(), out var instances))
        {
            foreach (var kvp in instances)
            {
                var inst = kvp.Value;
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
