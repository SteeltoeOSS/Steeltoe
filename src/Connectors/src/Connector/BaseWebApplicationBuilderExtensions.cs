// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data;
using System.Data.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Connector;

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
        IConfigurationSection section = builder.Configuration.GetSection(key);
        bool registerDefaultHealthContributor = ShouldRegisterDefaultHealthContributor(section);

        foreach (IConfigurationSection child in section.GetChildren())
        {
            string bindingName = child.Key;

            if (bindingName == ConnectionStringPostProcessor.DefaultBindingName)
            {
                builder.Services.Configure<TOptions>(child);

                if (registerDefaultHealthContributor)
                {
                    RegisterHealthContributor(builder.Services, string.Empty, createHealthContributor);
                }
            }
            else
            {
                builder.Services.Configure<TOptions>(bindingName, child);
                RegisterHealthContributor(builder.Services, bindingName, createHealthContributor);
            }
        }
    }

    private static bool ShouldRegisterDefaultHealthContributor(IConfigurationSection section)
    {
        IConfigurationSection[] childSections = section.GetChildren().ToArray();
        return childSections.Length == 1 && childSections[0].Key == ConnectionStringPostProcessor.DefaultBindingName;
    }

    private static void RegisterHealthContributor(IServiceCollection services, string bindingName,
        Func<IServiceProvider, string, IHealthContributor> createHealthContributor)
    {
        services.AddSingleton(typeof(IHealthContributor), serviceProvider => createHealthContributor(serviceProvider, bindingName));
    }

    public static IHealthContributor CreateRelationalHealthContributor<TOptions>(IServiceProvider serviceProvider, string bindingName, Type connectionType,
        string healthDisplayName, string healthHostNameKey)
        where TOptions : ConnectionStringOptions
    {
        var connection = (IDbConnection)ConnectionFactoryInvoker.CreateConnection<TOptions>(serviceProvider, bindingName, connectionType);
        string serviceName = $"{healthDisplayName}-{bindingName}";
        string hostName = GetHostNameFromConnectionString(connection.ConnectionString, healthHostNameKey);
        var logger = serviceProvider.GetRequiredService<ILogger<RelationalDbHealthContributor>>();

        return new RelationalDbHealthContributor(connection, serviceName, hostName, logger);
    }

    private static string GetHostNameFromConnectionString(string connectionString, string healthHostNameKey)
    {
        var builder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        return (string)builder[healthHostNameKey];
    }

    public static void RegisterConnectionFactory<TOptions>(IServiceCollection services, Type connectionType)
        where TOptions : ConnectionStringOptions
    {
        Type connectionFactoryType = ConnectionFactoryInvoker.MakeConnectionFactoryType<TOptions>(connectionType);

        services.AddSingleton(connectionFactoryType,
            serviceProvider => ConnectionFactoryInvoker.CreateConnectionFactory(serviceProvider, connectionFactoryType, connectionType));
    }
}
