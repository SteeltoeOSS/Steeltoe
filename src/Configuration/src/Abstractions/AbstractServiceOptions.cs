// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Options;

namespace Steeltoe.Extensions.Configuration;

public abstract class AbstractServiceOptions : AbstractOptions, IServicesInfo
{
    public virtual string ConfigurationPrefix { get; protected set; } = "services";

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

    public Dictionary<string, IEnumerable<Service>> Services { get; set; } = new();

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

    public IEnumerable<Service> GetServicesList()
    {
        var results = new List<Service>();

        if (Services != null)
        {
            foreach (KeyValuePair<string, IEnumerable<Service>> kvp in Services)
            {
                results.AddRange(kvp.Value);
            }
        }

        return results;
    }

    public IEnumerable<Service> GetInstancesOfType(string serviceType)
    {
        Services.TryGetValue(serviceType, out IEnumerable<Service> services);
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

        IConfigurationSection services = configuration.GetSection(ConfigurationPrefix);
        IConfigurationSection section = FindServiceSection(services, serviceName);

        if (section != null)
        {
            section.Bind(this);
        }
    }

    internal IConfigurationSection FindServiceSection(IConfigurationSection section, string serviceName)
    {
        IEnumerable<IConfigurationSection> children = section.GetChildren();

        foreach (IConfigurationSection child in children)
        {
            string name = child.GetValue<string>("name");

            if (serviceName == name)
            {
                return child;
            }
        }

        foreach (IConfigurationSection child in children)
        {
            IConfigurationSection result = FindServiceSection(child, serviceName);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
