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
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connector.PostgreSql;

namespace Steeltoe.Connector.PostgreSQL;

public static class PostgreSqlWebApplicationBuilderExtensions
{
    private const string BindingType = "postgresql";

    public static WebApplicationBuilder AddPostgreSql(this WebApplicationBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        RegisterConfigurationSource(builder.Configuration);
        RegisterNamedOptions(builder);
        RegisterConnectionFactory(builder.Services);

        return builder;
    }

    private static void RegisterConfigurationSource(IConfigurationBuilder configurationBuilder)
    {
        var source = new ConnectionStringConfigurationSource2();
        source.RegisterPostProcessor(new PostgreSqlConnectionStringPostProcessor());

        configurationBuilder.Add(source);
    }

    private static void RegisterNamedOptions(WebApplicationBuilder builder)
    {
        string key = ConfigurationPath.Combine(ConnectionStringPostProcessor.ServiceBindingsConfigurationKey, BindingType);
        IConfigurationSection section = builder.Configuration.GetSection(key);
        bool registerDefaultHealthContributor = ShouldRegisterDefaultHealthContributor(section);

        foreach (IConfigurationSection child in section.GetChildren())
        {
            string bindingName = child.Key;

            if (bindingName == ConnectionStringPostProcessor.DefaultBindingName)
            {
                builder.Services.Configure<PostgreSqlOptions>(child);

                if (registerDefaultHealthContributor)
                {
                    RegisterHealthContributor(builder.Services, string.Empty);
                }
            }
            else
            {
                builder.Services.Configure<PostgreSqlOptions>(bindingName, child);
                RegisterHealthContributor(builder.Services, bindingName);
            }
        }
    }

    private static bool ShouldRegisterDefaultHealthContributor(IConfigurationSection section)
    {
        IConfigurationSection[] childSections = section.GetChildren().ToArray();
        return childSections.Length == 1 && childSections[0].Key == ConnectionStringPostProcessor.DefaultBindingName;
    }

    private static void RegisterConnectionFactory(IServiceCollection services)
    {
        Type connectionFactoryType = typeof(ConnectionFactory<,>).MakeGenericType(typeof(PostgreSqlOptions), PostgreSqlTypeLocator.NpgsqlConnection);

        services.AddSingleton(connectionFactoryType, serviceProvider => CreateConnectionFactory(serviceProvider, connectionFactoryType));
    }

    private static object CreateConnectionFactory(IServiceProvider serviceProvider, Type connectionFactoryType)
    {
        Func<string, object> createConnection = connectionString => Activator.CreateInstance(PostgreSqlTypeLocator.NpgsqlConnection, connectionString);
        return Activator.CreateInstance(connectionFactoryType, serviceProvider, createConnection);
    }

    private static void RegisterHealthContributor(IServiceCollection services, string bindingName)
    {
        services.AddSingleton(typeof(IHealthContributor), serviceProvider => CreateHealthContributor(serviceProvider, bindingName));
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string bindingName)
    {
        IDbConnection connection = CreateDbConnection(serviceProvider, bindingName);
        string serviceName = $"PostgreSQL-{bindingName}";
        string hostName = GetHostNameFromConnectionString(connection.ConnectionString);
        var logger = serviceProvider.GetRequiredService<ILogger<RelationalDbHealthContributor>>();

        return new RelationalDbHealthContributor(connection, serviceName, hostName, logger);
    }

    private static IDbConnection CreateDbConnection(IServiceProvider serviceProvider, string bindingName)
    {
        Type connectionFactoryType = typeof(ConnectionFactory<,>).MakeGenericType(typeof(PostgreSqlOptions), PostgreSqlTypeLocator.NpgsqlConnection);
        object connectionFactory = serviceProvider.GetRequiredService(connectionFactoryType);
        MethodInfo getConnectionMethod = connectionFactoryType.GetMethod(nameof(ConnectionFactory<PostgreSqlOptions, object>.GetConnection))!;

        return (IDbConnection)getConnectionMethod.Invoke(connectionFactory, new object[]
        {
            bindingName
        });
    }

    private static string GetHostNameFromConnectionString(string connectionString)
    {
        var builder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        return (string)builder["host"];
    }
}
