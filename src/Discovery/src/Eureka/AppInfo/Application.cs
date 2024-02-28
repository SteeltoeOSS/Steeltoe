// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Concurrent;
using Steeltoe.Common;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.AppInfo;

public sealed class Application
{
    private readonly ConcurrentDictionary<string, InstanceInfo> _instanceMap = new();

    public string Name { get; }
    public IList<InstanceInfo> Instances => new List<InstanceInfo>(_instanceMap.Values);

    internal Application(string name)
        : this(name, Array.Empty<InstanceInfo>())
    {
    }

    internal Application(string name, ICollection<InstanceInfo> instances)
    {
        ArgumentGuard.NotNullOrEmpty(name);
        ArgumentGuard.NotNull(instances);
        ArgumentGuard.ElementsNotNull(instances);

        Name = name;

        foreach (InstanceInfo instance in instances)
        {
            Add(instance);
        }
    }

    internal InstanceInfo? GetInstance(string instanceId)
    {
        ArgumentGuard.NotNull(instanceId);

        return _instanceMap.GetValueOrDefault(instanceId);
    }

    public override string ToString()
    {
        return $"Application[Name={Name}, Instances={string.Join(',', Instances.Select(instance => instance.ToString()))}]";
    }

    internal void Add(InstanceInfo instance)
    {
        ArgumentGuard.NotNull(instance);

        if (!string.IsNullOrEmpty(instance.InstanceId))
        {
            _instanceMap[instance.InstanceId] = instance;
        }
        else if (!string.IsNullOrEmpty(instance.HostName))
        {
            _instanceMap[instance.HostName] = instance;
        }
    }

    internal void Remove(InstanceInfo instance)
    {
        ArgumentGuard.NotNull(instance);

        if (!_instanceMap.TryRemove(instance.InstanceId, out _))
        {
            _ = _instanceMap.TryRemove(instance.HostName, out _);
        }
    }

    internal static Application? FromJsonApplication(JsonApplication? jsonApplication)
    {
        if (jsonApplication == null || string.IsNullOrEmpty(jsonApplication.Name))
        {
            return null;
        }

        var app = new Application(jsonApplication.Name);

        if (jsonApplication.Instances != null)
        {
            foreach (JsonInstanceInfo? jsonInstance in jsonApplication.Instances)
            {
                var instance = InstanceInfo.FromJsonInstance(jsonInstance);

                if (instance != null)
                {
                    app.Add(instance);
                }
            }
        }

        return app;
    }
}
