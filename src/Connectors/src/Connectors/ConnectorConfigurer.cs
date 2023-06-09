// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Configuration.Kubernetes.ServiceBinding;

namespace Steeltoe.Connectors;

internal static class ConnectorConfigurer
{
    public static void Configure<TPostProcessor>(IConfigurationBuilder builder, Action<ConnectorConfigureOptions>? configureAction,
        TPostProcessor connectionStringPostProcessor)
        where TPostProcessor : ConnectionStringPostProcessor
    {
        if (!IsConfigured<TPostProcessor>(builder))
        {
            ConnectorConfigureOptions configureOptions = new();
            configureAction?.Invoke(configureOptions);

            builder.AddCloudFoundryServiceBindings();
            builder.AddKubernetesServiceBindings();

            RegisterPostProcessor(connectionStringPostProcessor, builder, configureOptions.DetectConfigurationChanges);
        }
    }

    private static bool IsConfigured<TPostProcessor>(IConfigurationBuilder builder)
        where TPostProcessor : ConnectionStringPostProcessor
    {
        return builder.Sources.OfType<ConnectionStringPostProcessorConfigurationSource>().Any(connectionStringSource =>
            connectionStringSource.PostProcessors.Any(postProcessor => postProcessor is TPostProcessor));
    }

    private static void RegisterPostProcessor(ConnectionStringPostProcessor connectionStringPostProcessor, IConfigurationBuilder builder,
        bool detectConfigurationChanges)
    {
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
