// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data;
using System.Data.Common;
using System.Reflection;
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

    public static void RegisterNamedOptions<TOptions>(WebApplicationBuilder builder, string bindingType, Type connectionType, string healthDisplayName,
        string healthHostNameKey)
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
                    RegisterHealthContributor<TOptions>(builder.Services, string.Empty, connectionType, healthDisplayName, healthHostNameKey);
                }
            }
            else
            {
                builder.Services.Configure<TOptions>(bindingName, child);
                RegisterHealthContributor<TOptions>(builder.Services, bindingName, connectionType, healthDisplayName, healthHostNameKey);
            }
        }
    }

    private static bool ShouldRegisterDefaultHealthContributor(IConfigurationSection section)
    {
        IConfigurationSection[] childSections = section.GetChildren().ToArray();
        return childSections.Length == 1 && childSections[0].Key == ConnectionStringPostProcessor.DefaultBindingName;
    }

    private static void RegisterHealthContributor<TOptions>(IServiceCollection services, string bindingName, Type connectionType, string healthDisplayName,
        string healthHostNameKey)
        where TOptions : ConnectionStringOptions
    {
        services.AddSingleton(typeof(IHealthContributor),
            serviceProvider => CreateHealthContributor<TOptions>(serviceProvider, bindingName, connectionType, healthDisplayName, healthHostNameKey));
    }

    private static IHealthContributor CreateHealthContributor<TOptions>(IServiceProvider serviceProvider, string bindingName, Type connectionType,
        string healthDisplayName, string healthHostNameKey)
        where TOptions : ConnectionStringOptions
    {
        IDbConnection connection = CreateDbConnection<TOptions>(serviceProvider, bindingName, connectionType);
        string serviceName = $"{healthDisplayName}-{bindingName}";
        string hostName = GetHostNameFromConnectionString(connection.ConnectionString, healthHostNameKey);
        var logger = serviceProvider.GetRequiredService<ILogger<RelationalDbHealthContributor>>();

        return new RelationalDbHealthContributor(connection, serviceName, hostName, logger);
    }

    private static IDbConnection CreateDbConnection<TOptions>(IServiceProvider serviceProvider, string bindingName, Type connectionType)
        where TOptions : ConnectionStringOptions
    {
        Type connectionFactoryType = typeof(ConnectionFactory<,>).MakeGenericType(typeof(TOptions), connectionType);
        object connectionFactory = serviceProvider.GetRequiredService(connectionFactoryType);
        MethodInfo getConnectionMethod = connectionFactoryType.GetMethod(nameof(ConnectionFactory<TOptions, object>.GetConnection))!;

        return (IDbConnection)getConnectionMethod.Invoke(connectionFactory, new object[]
        {
            bindingName
        });
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
        Type connectionFactoryType = typeof(ConnectionFactory<,>).MakeGenericType(typeof(TOptions), connectionType);

        services.AddSingleton(connectionFactoryType, serviceProvider => CreateConnectionFactory(serviceProvider, connectionFactoryType, connectionType));
    }

    private static object CreateConnectionFactory(IServiceProvider serviceProvider, Type connectionFactoryType, Type connectionType)
    {
        Func<string, object> createConnection = connectionString =>
        {
            try
            {
                return Activator.CreateInstance(connectionType, connectionString);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        };

        return Activator.CreateInstance(connectionFactoryType, serviceProvider, createConnection);
    }
}
