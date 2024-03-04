// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Common.Options;

namespace Steeltoe.Configuration;

/// <summary>
/// Represents a service in ASP.NET configuration, containing nested services grouped by type.
/// </summary>
/// <remarks>
/// Binds with an <see cref="IConfiguration" /> when instantiated.
/// </remarks>
public abstract class BaseServiceOptions : AbstractOptions
{
    protected virtual string ConfigurationPrefix => "services";

    /// <summary>
    /// Gets or sets the name of the service.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a label describing the type of service.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets a list of tags describing the service.
    /// </summary>
    public IList<string> Tags { get; } = new List<string>();

    /// <summary>
    /// Gets or sets the plan level at which the service is provisioned.
    /// </summary>
    public string? Plan { get; set; }

    public IDictionary<string, IList<Service>> Services { get; } = new Dictionary<string, IList<Service>>();

    // This constructor is for use with IOptions.
    protected BaseServiceOptions()
    {
    }

    protected BaseServiceOptions(IConfiguration configuration)
        : this(configuration, null)
    {
    }

    protected BaseServiceOptions(IConfiguration configuration, string? sectionPrefix)
        : base(configuration, sectionPrefix)
    {
    }

    /// <summary>
    /// Retrieves a flattened list of all services for all types.
    /// </summary>
    /// <returns>
    /// The complete list of services known to the application.
    /// </returns>
    public IList<Service> GetAllServices()
    {
        var services = new List<Service>();

        foreach (KeyValuePair<string, IList<Service>> pair in Services)
        {
            services.AddRange(pair.Value);
        }

        return services;
    }

    /// <summary>
    /// Retrieves a list of all services of a given service type.
    /// </summary>
    /// <param name="serviceType">
    /// The type to find services for. May be platform/broker/version dependent.
    /// </param>
    /// <remarks>
    /// Sample values include: p-mysql, azure-mysql-5-7, p-configserver, p.configserver.
    /// </remarks>
    /// <returns>
    /// A list of services configured under the given type.
    /// </returns>
    public IList<Service> GetServicesOfType(string serviceType)
    {
        ArgumentGuard.NotNullOrEmpty(serviceType);

        return Services.TryGetValue(serviceType, out IList<Service>? services) ? services : Array.Empty<Service>();
    }

    /// <summary>
    /// Attempts to find the specified service and bind it to the configuration.
    /// </summary>
    /// <param name="configuration">
    /// The configuration to search for a section or value matching <paramref name="serviceName" />.
    /// </param>
    /// <param name="serviceName">
    /// Name of the service or section to find.
    /// </param>
    public void Bind(IConfiguration configuration, string serviceName)
    {
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNullOrEmpty(serviceName);

        IConfigurationSection services = configuration.GetSection(ConfigurationPrefix);
        IConfigurationSection? section = FindServiceSection(services, serviceName);

        section?.Bind(this);
    }

    private IConfigurationSection? FindServiceSection(IConfigurationSection section, string serviceName)
    {
        IConfigurationSection[] children = section.GetChildren().ToArray();

        foreach (IConfigurationSection child in children)
        {
            string? name = child.GetValue<string>("name");

            if (serviceName == name)
            {
                return child;
            }
        }

        foreach (IConfigurationSection child in children)
        {
            IConfigurationSection? nestedSection = FindServiceSection(child, serviceName);

            if (nestedSection != null)
            {
                return nestedSection;
            }
        }

        return null;
    }
}
