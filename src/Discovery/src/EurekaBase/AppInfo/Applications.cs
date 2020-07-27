// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Transport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Steeltoe.Discovery.Eureka.AppInfo
{
    public class Applications
    {
        private object _addRemoveInstanceLock = new object();

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
                sb.Append(kvp.Value.ToString());
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

            ApplicationMap.AddOrUpdate(app.Name.ToUpperInvariant(), app, (key, existing) => { return app; });
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
                var addressUppper = address.ToUpperInvariant();
                dict.TryGetValue(addressUppper, out var instances);
                InstanceInfo removed = null;
                if (instances != null)
                {
                    instances.TryRemove(info.InstanceId, out removed);
                    if (instances.Count <= 0)
                    {
                        dict.TryRemove(addressUppper, out instances);
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

                    switch (instance.Actiontype)
                    {
                        case ActionType.ADDED:
                        case ActionType.MODIFIED:
                            // logger.debug("Added instance {} to the existing apps in region {}", instance.getId(), instanceRegion);
                            existingApp.Add(instance);
                            AddInstanceToVip(instance);
                            break;
                        case ActionType.DELETED:
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
                    if (!statusMap.TryGetValue(inst.Status.ToString(), out var count))
                    {
                        statusMap.Add(inst.Status.ToString(), 1);
                    }
                    else
                    {
                        statusMap[inst.Status.ToString()] = count + 1;
                    }
                }
            }

            var query = statusMap.OrderBy(kvp => kvp.Key);
            var hashcodeBuilder = new StringBuilder();
            foreach (var entry in query)
            {
                hashcodeBuilder.Append(entry.Key.ToString() + "_" + entry.Value.ToString() + "_");
            }

            return hashcodeBuilder.ToString();
        }

        internal ConcurrentDictionary<string, Application> ApplicationMap { get; } = new ConcurrentDictionary<string, Application>();

        internal ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> VirtualHostInstanceMap { get; } = new ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>>();

        internal ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> SecureVirtualHostInstanceMap { get; } = new ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>>();

        internal static Applications FromJsonApplications(JsonApplications japps)
        {
            var apps = new Applications();
            if (japps != null)
            {
                apps.Version = japps.VersionDelta;
                apps.AppsHashCode = japps.AppsHashCode;

                if (japps.Applications != null)
                {
                    foreach (var japp in japps.Applications)
                    {
                        var app = Application.FromJsonApplication(japp);
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
                        if (inst.Status == InstanceStatus.UP)
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
}
