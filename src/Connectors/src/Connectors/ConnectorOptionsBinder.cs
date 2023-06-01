// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Connectors;

internal static class ConnectorOptionsBinder
{
    /// <summary>
    /// Binds configuration keys below steeltoe:service-bindings to <see cref="ConnectionStringOptions" /> and registers health contributors.
    /// </summary>
    /// <returns>
    /// The set of service binding names, which corresponds to the bound named options.
    /// </returns>
    public static IReadOnlySet<string> RegisterNamedOptions<TOptions>(IServiceCollection services, IConfiguration configuration, string bindingType,
        ConnectorCreateHealthContributor? createHealthContributor)
        where TOptions : ConnectionStringOptions
    {
        string key = ConfigurationPath.Combine(ConnectionStringPostProcessor.ServiceBindingsConfigurationKey, bindingType);
        IConfigurationSection[] childSections = configuration.GetSection(key).GetChildren().ToArray();

        bool registerDefaultHealthContributor = !ContainsNamedServiceBindings(childSections);
        bool defaultHealthContributorRegistered = false;

        foreach (IConfigurationSection childSection in childSections)
        {
            string bindingName = childSection.Key;

            if (bindingName == ConnectionStringPostProcessor.DefaultBindingName)
            {
                services.Configure<TOptions>(childSection);

                if (registerDefaultHealthContributor)
                {
                    RegisterHealthContributor(services, string.Empty, createHealthContributor);
                    defaultHealthContributorRegistered = true;
                }
            }
            else
            {
                services.Configure<TOptions>(bindingName, childSection);
                RegisterHealthContributor(services, bindingName, createHealthContributor);
            }
        }

        if (registerDefaultHealthContributor && !defaultHealthContributorRegistered)
        {
            RegisterHealthContributor(services, string.Empty, createHealthContributor);
        }

        return GetNamedOptions(childSections);
    }

    private static bool ContainsNamedServiceBindings(IConfigurationSection[] sections)
    {
        if (sections.Length == 0)
        {
            return false;
        }

        if (sections is [{ Key: ConnectionStringPostProcessor.DefaultBindingName }])
        {
            return false;
        }

        return true;
    }

    private static void RegisterHealthContributor(IServiceCollection services, string serviceBindingName,
        ConnectorCreateHealthContributor? createHealthContributor)
    {
        if (createHealthContributor != null)
        {
            services.AddSingleton(typeof(IHealthContributor), serviceProvider => createHealthContributor(serviceProvider, serviceBindingName));
        }
    }

    private static HashSet<string> GetNamedOptions(IConfigurationSection[] childSections)
    {
        return childSections.Select(section => section.Key == ConnectionStringPostProcessor.DefaultBindingName ? string.Empty : section.Key).ToHashSet();
    }
}
