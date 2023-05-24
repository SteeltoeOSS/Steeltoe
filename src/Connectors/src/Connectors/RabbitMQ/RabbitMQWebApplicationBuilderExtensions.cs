// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

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
using Steeltoe.Connectors.RabbitMQ.RuntimeTypeAccess;
using Steeltoe.Connectors.RuntimeTypeAccess;

namespace Steeltoe.Connectors.RabbitMQ;

public delegate IDisposable CreateRabbitConnection(RabbitMQOptions options, string serviceBindingName);

public static class RabbitMQWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddRabbitMQ(this WebApplicationBuilder builder, CreateRabbitConnection? createRabbitConnection = null)
    {
        return AddRabbitMQ(builder, new RabbitMQPackageResolver(), createRabbitConnection);
    }

    private static WebApplicationBuilder AddRabbitMQ(this WebApplicationBuilder builder, RabbitMQPackageResolver packageResolver,
        CreateRabbitConnection? createRabbitConnection)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        RegisterPostProcessors(builder.Configuration);

        Func<IServiceProvider, string, IHealthContributor> createHealthContributor = (serviceProvider, serviceBindingName) =>
            CreateHealthContributor(serviceProvider, serviceBindingName, packageResolver);

        BaseWebApplicationBuilderExtensions.RegisterNamedOptions<RabbitMQOptions>(builder, "rabbitmq", createHealthContributor);

        Func<RabbitMQOptions, string, object> createConnection = (options, serviceBindingName) => createRabbitConnection != null
            ? createRabbitConnection(options, serviceBindingName)
            : CreateDefaultRabbitConnection(options, packageResolver);

        ConnectorFactoryShim<RabbitMQOptions>.Register(builder.Services, packageResolver.ConnectionInterface.Type, true, createConnection);

        return builder;
    }

    private static void RegisterPostProcessors(IConfigurationBuilder builder)
    {
        builder.AddCloudFoundryServiceBindings();
        CloudFoundryServiceBindingConfigurationSource cloudFoundrySource = builder.Sources.OfType<CloudFoundryServiceBindingConfigurationSource>().First();
        cloudFoundrySource.RegisterPostProcessor(new RabbitMQCloudFoundryPostProcessor());

        builder.AddKubernetesServiceBindings();
        KubernetesServiceBindingConfigurationSource kubernetesSource = builder.Sources.OfType<KubernetesServiceBindingConfigurationSource>().First();
        kubernetesSource.RegisterPostProcessor(new RabbitMQKubernetesPostProcessor());

        var connectionStringPostProcessor = new RabbitMQConnectionStringPostProcessor();
        var connectionStringSource = new ConnectionStringPostProcessorConfigurationSource();
        connectionStringSource.RegisterPostProcessor(connectionStringPostProcessor);
        builder.Add(connectionStringSource);
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string serviceBindingName,
        RabbitMQPackageResolver packageResolver)
    {
        ConnectorFactoryShim<RabbitMQOptions> connectorFactoryShim =
            ConnectorFactoryShim<RabbitMQOptions>.FromServiceProvider(serviceProvider, packageResolver.ConnectionInterface.Type);

        ConnectorShim<RabbitMQOptions> connectorShim = connectorFactoryShim.GetNamed(serviceBindingName);

        object connection = connectorShim.GetConnection();
        string hostName = GetHostNameFromConnectionString(connectorShim.Options.ConnectionString);
        var logger = serviceProvider.GetRequiredService<ILogger<RabbitMQHealthContributor>>();

        return new RabbitMQHealthContributor(connection, $"RabbitMQ-{serviceBindingName}", hostName, logger);
    }

    private static string GetHostNameFromConnectionString(string? connectionString)
    {
        var builder = new RabbitMQConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        return (string?)builder["host"] ?? "localhost";
    }

    private static IDisposable CreateDefaultRabbitConnection(RabbitMQOptions options, RabbitMQPackageResolver packageResolver)
    {
        // In RabbitMQ, connections are long-lived and auto-recover from network failures. Channels are multiplexed over a single connection.
        var connectionFactoryShim = ConnectionFactoryShim.CreateInstance(packageResolver);

        if (!string.IsNullOrEmpty(options.ConnectionString))
        {
            connectionFactoryShim.Uri = new Uri(options.ConnectionString);
        }

        ConnectionInterfaceShim connectionInterfaceShim = connectionFactoryShim.CreateConnection();
        return connectionInterfaceShim.Instance;
    }
}
