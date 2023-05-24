// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Data.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding.PostProcessors;
using Steeltoe.Configuration.Kubernetes.ServiceBinding;
using Steeltoe.Configuration.Kubernetes.ServiceBinding.PostProcessors;
using Steeltoe.Connectors.MySql.RuntimeTypeAccess;
using Steeltoe.Connectors.RuntimeTypeAccess;

namespace Steeltoe.Connectors.MySql;

public static class MySqlWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddMySql(this WebApplicationBuilder builder)
    {
        return AddMySql(builder, new MySqlPackageResolver());
    }

    internal static WebApplicationBuilder AddMySql(this WebApplicationBuilder builder, MySqlPackageResolver packageResolver)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        RegisterPostProcessors(builder.Configuration, packageResolver);

        Func<IServiceProvider, string, IHealthContributor> createHealthContributor = (serviceProvider, serviceBindingName) =>
            CreateHealthContributor(serviceProvider, serviceBindingName, packageResolver);

        BaseWebApplicationBuilderExtensions.RegisterNamedOptions<MySqlOptions>(builder, "mysql", createHealthContributor);

        Func<MySqlOptions, string, object> createConnection = (options, _) =>
            MySqlConnectionShim.CreateInstance(packageResolver, options.ConnectionString).Instance;

        ConnectorFactoryShim<MySqlOptions>.Register(builder.Services, packageResolver.MySqlConnectionClass.Type, false, createConnection);

        return builder;
    }

    private static void RegisterPostProcessors(IConfigurationBuilder builder, MySqlPackageResolver packageResolver)
    {
        builder.AddCloudFoundryServiceBindings();
        CloudFoundryServiceBindingConfigurationSource cloudFoundrySource = builder.Sources.OfType<CloudFoundryServiceBindingConfigurationSource>().First();
        cloudFoundrySource.RegisterPostProcessor(new MySqlCloudFoundryPostProcessor());

        builder.AddKubernetesServiceBindings();
        KubernetesServiceBindingConfigurationSource kubernetesSource = builder.Sources.OfType<KubernetesServiceBindingConfigurationSource>().First();
        kubernetesSource.RegisterPostProcessor(new MySqlKubernetesPostProcessor());

        var connectionStringPostProcessor = new MySqlConnectionStringPostProcessor(packageResolver);
        var connectionStringSource = new ConnectionStringPostProcessorConfigurationSource();
        connectionStringSource.RegisterPostProcessor(connectionStringPostProcessor);
        builder.Add(connectionStringSource);
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string serviceBindingName, MySqlPackageResolver packageResolver)
    {
        ConnectorFactoryShim<MySqlOptions> connectorFactoryShim =
            ConnectorFactoryShim<MySqlOptions>.FromServiceProvider(serviceProvider, packageResolver.MySqlConnectionClass.Type);

        ConnectorShim<MySqlOptions> connectorShim = connectorFactoryShim.GetNamed(serviceBindingName);

        var connection = (DbConnection)connectorShim.GetConnection();
        string hostName = GetHostNameFromConnectionString(packageResolver, connectorShim.Options.ConnectionString);
        var logger = serviceProvider.GetRequiredService<ILogger<RelationalDbHealthContributor>>();

        return new RelationalDbHealthContributor(connection, $"MySQL-{serviceBindingName}", hostName, logger);
    }

    private static string GetHostNameFromConnectionString(MySqlPackageResolver packageResolver, string? connectionString)
    {
        var connectionStringBuilderShim = MySqlConnectionStringBuilderShim.CreateInstance(packageResolver);
        connectionStringBuilderShim.Instance.ConnectionString = connectionString;
        return (string)connectionStringBuilderShim.Instance["host"];
    }
}
