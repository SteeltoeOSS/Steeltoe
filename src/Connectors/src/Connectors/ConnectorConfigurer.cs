// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Configuration;
using Steeltoe.Configuration.CloudFoundry.ServiceBindings;
using Steeltoe.Configuration.Kubernetes.ServiceBindings;
using IServiceBindingsReader = Steeltoe.Configuration.CloudFoundry.ServiceBindings.IServiceBindingsReader;

namespace Steeltoe.Connectors;

internal static class ConnectorConfigurer
{
    private static readonly Predicate<string> DefaultIgnoreKeyPredicate = _ => false;

    public static void Configure<TPostProcessor>(IConfigurationBuilder builder, Action<ConnectorConfigureOptionsBuilder> configureAction,
        TPostProcessor connectionStringPostProcessor, IServiceBindingsReader? serviceBindingsReader, ILoggerFactory loggerFactory)
        where TPostProcessor : ConnectionStringPostProcessor
    {
        if (!IsConfigured<TPostProcessor>(builder))
        {
            var optionsBuilder = new ConnectorConfigureOptionsBuilder();
            configureAction.Invoke(optionsBuilder);

            if (!optionsBuilder.SkipDefaultServiceBindings)
            {
                builder.AddCloudFoundryServiceBindings(DefaultIgnoreKeyPredicate, serviceBindingsReader, optionsBuilder.CloudFoundryBrokerTypes, loggerFactory);
                builder.AddKubernetesServiceBindings();
            }

            RegisterPostProcessor(connectionStringPostProcessor, builder, optionsBuilder.DetectConfigurationChanges);
        }
    }

    private static bool IsConfigured<TPostProcessor>(IConfigurationBuilder builder)
        where TPostProcessor : ConnectionStringPostProcessor
    {
        return builder.EnumerateSources<ConnectionStringPostProcessorConfigurationSource>().Any(connectionStringSource =>
            connectionStringSource.PostProcessors.Any(postProcessor => postProcessor is TPostProcessor));
    }

    private static void RegisterPostProcessor(ConnectionStringPostProcessor connectionStringPostProcessor, IConfigurationBuilder builder,
        bool detectConfigurationChanges)
    {
        var connectionStringSource = new ConnectionStringPostProcessorConfigurationSource(detectConfigurationChanges);
        connectionStringSource.RegisterPostProcessor(connectionStringPostProcessor);

        builder.Add(connectionStringSource);
    }
}
