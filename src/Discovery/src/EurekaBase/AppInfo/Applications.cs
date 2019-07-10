// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
        private ConcurrentDictionary<string, Application> _applicationMap = new ConcurrentDictionary<string, Application>();
        private ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> _virtHostInstanceMap = new ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>>();
        private ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> _secureVirtHostInstanceMap = new ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>>();

        private object _addRemoveInstanceLock = new object();

        public string AppsHashCode { get; internal set; }

        public long Version { get; internal set; }

        public bool ReturnUpInstancesOnly { get; set; }

        public IList<Application> GetRegisteredApplications()
        {
            return new List<Application>(_applicationMap.Values);
        }

        public Application GetRegisteredApplication(string appName)
        {
            if (string.IsNullOrEmpty(appName))
            {
                throw new ArgumentException(nameof(appName));
            }

            _applicationMap.TryGetValue(appName.ToUpperInvariant(), out Application result);
            return result;
        }

        public IList<InstanceInfo> GetInstancesBySecureVirtualHostName(string secureVirtualHostName)
        {
            if (string.IsNullOrEmpty(secureVirtualHostName))
            {
                throw new ArgumentException(nameof(secureVirtualHostName));
            }

            return DoGetByVirtualHostName(secureVirtualHostName, _secureVirtHostInstanceMap);
        }

        public IList<InstanceInfo> GetInstancesByVirtualHostName(string virtualHostName)
        {
            if (string.IsNullOrEmpty(virtualHostName))
            {
                throw new ArgumentException(nameof(virtualHostName));
            }

            return DoGetByVirtualHostName(virtualHostName, _virtHostInstanceMap);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("Applications[");
            foreach (var kvp in this.ApplicationMap)
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

            _applicationMap.AddOrUpdate(app.Name.ToUpperInvariant(), app, (key, existing) => { return app; });
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
                AddInstanceToVip(inst.VipAddress, inst, _virtHostInstanceMap);
            }

            if (!string.IsNullOrEmpty(inst.SecureVipAddress))
            {
                AddInstanceToVip(inst.SecureVipAddress, inst, _secureVirtHostInstanceMap);
            }
        }

        internal void AddInstanceToVip(string address, InstanceInfo info, ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> dict)
        {
            lock (_addRemoveInstanceLock)
            {
                string addressUppper = address.ToUpperInvariant();
                dict.TryGetValue(addressUppper, out ConcurrentDictionary<string, InstanceInfo> instances);
                if (instances == null)
                {
                    instances = dict[addressUppper] = new ConcurrentDictionary<string, InstanceInfo>();
                }

                instances[info.InstanceId] = info;
            }
        }

        internal void RemoveInstanceFromVip(InstanceInfo inst)
        {
            if (!string.IsNullOrEmpty(inst.VipAddress))
            {
                RemoveInstanceFromVip(inst.VipAddress, inst, _virtHostInstanceMap);
            }

            if (!string.IsNullOrEmpty(inst.SecureVipAddress))
            {
                RemoveInstanceFromVip(inst.SecureVipAddress, inst, _secureVirtHostInstanceMap);
            }
        }

        internal void RemoveInstanceFromVip(string address, InstanceInfo info, ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> dict)
        {
            lock (_addRemoveInstanceLock)
            {
                string addressUppper = address.ToUpperInvariant();
                dict.TryGetValue(addressUppper, out ConcurrentDictionary<string, InstanceInfo> instances);
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
            Dictionary<string, int> statusMap = new Dictionary<string, int>();
            foreach (var app in GetRegisteredApplications())
            {
                foreach (var inst in app.Instances)
                {
                    if (!statusMap.TryGetValue(inst.Status.ToString(), out int count))
                    {
                        statusMap.Add(inst.Status.ToString(), 1);
                    }
                    else
                    {
                        statusMap[inst.Status.ToString()] = count + 1;
                    }
                }
            }

            IOrderedEnumerable<KeyValuePair<string, int>> query = statusMap.OrderBy(kvp => kvp.Key);
            var hashcodeBuilder = new StringBuilder();
            foreach (var entry in query)
            {
                hashcodeBuilder.Append(entry.Key.ToString() + "_" + entry.Value.ToString() + "_");
            }

            return hashcodeBuilder.ToString();
        }

        internal ConcurrentDictionary<string, Application> ApplicationMap
        {
            get
            {
                return _applicationMap;
            }
        }

        internal ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> VirtualHostInstanceMap
        {
            get
            {
                return _virtHostInstanceMap;
            }
        }

        internal ConcurrentDictionary<string, ConcurrentDictionary<string, InstanceInfo>> SecureVirtualHostInstanceMap
        {
            get
            {
                return _secureVirtHostInstanceMap;
            }
        }

        internal static Applications FromJsonApplications(JsonApplications japps)
        {
            Applications apps = new Applications();
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
            List<InstanceInfo> result = new List<InstanceInfo>();
            if (dict.TryGetValue(name.ToUpperInvariant(), out ConcurrentDictionary<string, InstanceInfo> instances))
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
