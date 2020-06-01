// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Transport;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Discovery.Eureka.AppInfo
{
    public class Application
    {
        private ConcurrentDictionary<string, InstanceInfo> _instanceMap = new ConcurrentDictionary<string, InstanceInfo>();

        public string Name { get; internal set; }

        public int Count
        {
            get
            {
                return _instanceMap.Count;
            }
        }

        public IList<InstanceInfo> Instances
        {
            get
            {
                return new List<InstanceInfo>(_instanceMap.Values);
            }
        }

        public InstanceInfo GetInstance(string instanceId)
        {
            _instanceMap.TryGetValue(instanceId, out InstanceInfo result);
            return result;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("Application[");
            sb.Append("Name=" + Name);
            sb.Append(",Instances=");
            foreach (var inst in Instances)
            {
                sb.Append(inst.ToString());
                sb.Append(",");
            }

            sb.Append("]");
            return sb.ToString();
        }

        internal Application(string name)
        {
            Name = name;
        }

        internal Application(string name, IList<InstanceInfo> instances)
        {
            Name = name;
            foreach (InstanceInfo info in instances)
            {
                Add(info);
            }
        }

        internal void Add(InstanceInfo info)
        {
            if (!string.IsNullOrEmpty(info.InstanceId))
            {
                _instanceMap[info.InstanceId] = info;
            }
            else if (!string.IsNullOrEmpty(info.HostName))
            {
                _instanceMap[info.HostName] = info;
            }
        }

        internal void Remove(InstanceInfo info)
        {
            if (!_instanceMap.TryRemove(info.InstanceId, out InstanceInfo removed))
            {
                _instanceMap.TryRemove(info.HostName, out removed);
            }
        }

        internal ConcurrentDictionary<string, InstanceInfo> InstanceMap
        {
            get
            {
                return _instanceMap;
            }
        }

        internal static Application FromJsonApplication(JsonApplication japp)
        {
            if (japp == null)
            {
                return null;
            }

            Application app = new Application(japp.Name);
            if (japp.Instances != null)
            {
                foreach (var instance in japp.Instances)
                {
                    var inst = InstanceInfo.FromJsonInstance(instance);
                    app.Add(inst);
                }
            }

            return app;
        }
    }
}
