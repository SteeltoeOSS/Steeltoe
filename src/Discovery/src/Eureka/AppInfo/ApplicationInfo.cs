// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Text.Json;
using Steeltoe.Common;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.AppInfo;

/// <summary>
/// Represents an application in Eureka server.
/// </summary>
public sealed class ApplicationInfo
{
    private readonly ConcurrentDictionary<string, InstanceInfo> _instanceMap = new();

    public string Name { get; }
    public IReadOnlyList<InstanceInfo> Instances => new List<InstanceInfo>(_instanceMap.Values);

    internal ApplicationInfo(string name)
        : this(name, Array.Empty<InstanceInfo>())
    {
    }

    internal ApplicationInfo(string name, ICollection<InstanceInfo> instances)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(instances);
        ArgumentGuard.ElementsNotNull(instances);

        Name = name;

        foreach (InstanceInfo instance in instances)
        {
            Add(instance);
        }
    }

    internal InstanceInfo? GetInstance(string instanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        return _instanceMap.GetValueOrDefault(instanceId);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, DebugSerializerOptions.Instance);
    }

    internal void Add(InstanceInfo instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        _instanceMap[instance.InstanceId] = instance;
    }

    internal void Remove(InstanceInfo instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        _instanceMap.TryRemove(instance.InstanceId, out _);
    }

    internal static ApplicationInfo? FromJson(JsonApplication? jsonApplication, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (jsonApplication == null || string.IsNullOrWhiteSpace(jsonApplication.Name))
        {
            return null;
        }

        var application = new ApplicationInfo(jsonApplication.Name);

        if (jsonApplication.Instances != null)
        {
            foreach (JsonInstanceInfo? jsonInstance in jsonApplication.Instances)
            {
                InstanceInfo? instance = InstanceInfo.FromJson(jsonInstance, timeProvider);

                if (instance != null)
                {
                    application.Add(instance);
                }
            }
        }

        return application;
    }
}
