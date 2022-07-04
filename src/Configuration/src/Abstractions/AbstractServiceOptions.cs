// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Options;
using System;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Configuration;

public abstract class AbstractServiceOptions : AbstractOptions, IServicesInfo
{
    public virtual string ConfigurationPrefix { get; protected set; } = "services";

    // This constructor is for use with IOptions
    protected AbstractServiceOptions()
    {
    }

    protected AbstractServiceOptions(IConfigurationRoot root, string sectionPrefix = "")
        : base(root, sectionPrefix)
    {
    }

    protected AbstractServiceOptions(IConfiguration config, string sectionPrefix = "")
        : base(config, sectionPrefix)
    {
    }

    /// <summary>
    /// Gets or sets the name of the service instance.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets a label describing the type of service.
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Gets or sets the plan level at which the service is provisioned.
    /// </summary>
    public IEnumerable<string> Tags { get; set; }

    /// <summary>
    /// Gets or sets a list of tags describing the service.
    /// </summary>
    public string Plan { get; set; }

    public Dictionary<string, IEnumerable<Service>> Services { get; set; } = new ();

    public IEnumerable<Service> GetServicesList()
    {
        var results = new List<Service>();
        if (Services != null)
        {
            foreach (var kvp in Services)
            {
                results.AddRange(kvp.Value);
            }
        }

        return results;
    }

    public IEnumerable<Service> GetInstancesOfType(string serviceType)
    {
        Services.TryGetValue(serviceType, out var services);
        return services ?? new List<Service>();
    }

    public void Bind(IConfiguration configuration, string serviceName)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (string.IsNullOrEmpty(serviceName))
        {
            throw new ArgumentException(nameof(serviceName));
        }

        var services = configuration.GetSection(ConfigurationPrefix);
        var section = FindServiceSection(services, serviceName);

        if (section != null)
        {
            section.Bind(this);
        }
    }

    internal IConfigurationSection FindServiceSection(IConfigurationSection section, string serviceName)
    {
        var children = section.GetChildren();
        foreach (var child in children)
        {
            var name = child.GetValue<string>("name");
            if (serviceName == name)
            {
                return child;
            }
        }

        foreach (var child in children)
        {
            var result = FindServiceSection(child, serviceName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
