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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Discovery.Eureka.AppInfo
{
    public class Application
    {
        public string Name { get; internal set; }

        public int Count => InstanceMap.Count;

        public IList<InstanceInfo> Instances => new List<InstanceInfo>(InstanceMap.Values);

        public InstanceInfo GetInstance(string instanceId)
        {
            InstanceMap.TryGetValue(instanceId, out var result);
            return result;
        }

        public override string ToString()
        {
            var sb = new StringBuilder("Application[");
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
            foreach (var info in instances)
            {
                Add(info);
            }
        }

        internal void Add(InstanceInfo info)
        {
            if (!string.IsNullOrEmpty(info.InstanceId))
            {
                InstanceMap[info.InstanceId] = info;
            }
            else if (!string.IsNullOrEmpty(info.HostName))
            {
                InstanceMap[info.HostName] = info;
            }
        }

        internal void Remove(InstanceInfo info)
        {
            if (!InstanceMap.TryRemove(info.InstanceId, out var removed))
            {
                InstanceMap.TryRemove(info.HostName, out removed);
            }
        }

        internal ConcurrentDictionary<string, InstanceInfo> InstanceMap { get; } = new ConcurrentDictionary<string, InstanceInfo>();

        internal static Application FromJsonApplication(JsonApplication japp)
        {
            if (japp == null)
            {
                return null;
            }

            var app = new Application(japp.Name);
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
