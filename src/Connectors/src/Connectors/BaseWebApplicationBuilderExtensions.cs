// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Connectors;

internal static class BaseWebApplicationBuilderExtensions
{
    public static void RegisterConfigurationSource(IConfigurationBuilder configurationBuilder, ConnectionStringPostProcessor postProcessor)
    {
        var source = new ConnectionStringConfigurationSource2();
        source.RegisterPostProcessor(postProcessor);

        configurationBuilder.Add(source);
    }

    public static void RegisterNamedOptions<TOptions>(WebApplicationBuilder builder, string bindingType,
        Func<IServiceProvider, string, IHealthContributor> createHealthContributor)
        where TOptions : ConnectionStringOptions
    {
        string key = ConfigurationPath.Combine(ConnectionStringPostProcessor.ServiceBindingsConfigurationKey, bindingType);
        IConfigurationSection[] childSections = builder.Configuration.GetSection(key).GetChildren().ToArray();

        bool registerDefaultHealthContributor = !ContainsNamedServiceBindings(childSections);
        bool defaultHealthContributorRegistered = false;

        foreach (IConfigurationSection childSection in childSections)
        {
            string bindingName = childSection.Key;

            if (bindingName == ConnectionStringPostProcessor.DefaultBindingName)
            {
                builder.Services.Configure<TOptions>(childSection);

                if (registerDefaultHealthContributor)
                {
                    RegisterHealthContributor(builder.Services, string.Empty, createHealthContributor);
                    defaultHealthContributorRegistered = true;
                }
            }
            else
            {
                builder.Services.Configure<TOptions>(bindingName, childSection);
                RegisterHealthContributor(builder.Services, bindingName, createHealthContributor);
            }
        }

        if (registerDefaultHealthContributor && !defaultHealthContributorRegistered)
        {
            RegisterHealthContributor(builder.Services, string.Empty, createHealthContributor);
        }
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
        Func<IServiceProvider, string, IHealthContributor> createHealthContributor)
    {
        services.AddSingleton(typeof(IHealthContributor), serviceProvider => createHealthContributor(serviceProvider, serviceBindingName));
    }
}
