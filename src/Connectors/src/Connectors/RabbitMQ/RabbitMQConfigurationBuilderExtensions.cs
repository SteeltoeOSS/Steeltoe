// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Configuration.Kubernetes.ServiceBinding;

namespace Steeltoe.Connectors.RabbitMQ;

public static class RabbitMQConfigurationBuilderExtensions
{
    public static IConfigurationBuilder ConfigureRabbitMQ(this IConfigurationBuilder builder)
    {
        return ConfigureRabbitMQ(builder, null);
    }

    public static IConfigurationBuilder ConfigureRabbitMQ(this IConfigurationBuilder builder, Action<ConnectorConfigureOptions>? configureAction)
    {
        ArgumentGuard.NotNull(builder);

        if (!IsConfigured(builder))
        {
            ConnectorConfigureOptions configureOptions = new();
            configureAction?.Invoke(configureOptions);

            RegisterPostProcessors(builder, configureOptions.DetectConfigurationChanges);
        }

        return builder;
    }

    private static bool IsConfigured(IConfigurationBuilder builder)
    {
        return builder.Sources.OfType<ConnectionStringPostProcessorConfigurationSource>().Any(connectionStringSource =>
            connectionStringSource.PostProcessors.Any(postProcessor => postProcessor is RabbitMQConnectionStringPostProcessor));
    }

    private static void RegisterPostProcessors(IConfigurationBuilder builder, bool detectConfigurationChanges)
    {
        builder.AddCloudFoundryServiceBindings();
        builder.AddKubernetesServiceBindings();

        var connectionStringPostProcessor = new RabbitMQConnectionStringPostProcessor();
        var connectionStringSource = new ConnectionStringPostProcessorConfigurationSource(detectConfigurationChanges);
        connectionStringSource.RegisterPostProcessor(connectionStringPostProcessor);

        if (builder is ConfigurationManager configurationManager)
        {
            connectionStringSource.CaptureConfigurationManager(configurationManager);
            connectionStringPostProcessor.CaptureConfigurationManager(configurationManager);
        }

        builder.Add(connectionStringSource);
    }
}
