// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Text;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.AppInfo;

public sealed class Application
{
    internal ConcurrentDictionary<string, InstanceInfo> InstanceMap { get; } = new();

    public string Name { get; }
    public int Count => InstanceMap.Count;
    public IList<InstanceInfo> Instances => new List<InstanceInfo>(InstanceMap.Values);

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

    public InstanceInfo GetInstance(string instanceId)
    {
        InstanceMap.TryGetValue(instanceId, out InstanceInfo result);
        return result;
    }

    public override string ToString()
    {
        var sb = new StringBuilder("Application[");
        sb.Append($"Name={Name}");
        sb.Append(",Instances=");

        foreach (InstanceInfo instance in Instances)
        {
            sb.Append(instance);
            sb.Append(',');
        }

        sb.Append(']');
        return sb.ToString();
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
        if (!InstanceMap.TryRemove(info.InstanceId, out _))
        {
            InstanceMap.TryRemove(info.HostName, out _);
        }
    }

    internal static Application FromJsonApplication(JsonApplication application)
    {
        if (application == null)
        {
            return null;
        }

        var app = new Application(application.Name);

        if (application.Instances != null)
        {
            foreach (JsonInstanceInfo jsonInstance in application.Instances)
            {
                var instance = InstanceInfo.FromJsonInstance(jsonInstance);
                app.Add(instance);
            }
        }

        return app;
    }
}
