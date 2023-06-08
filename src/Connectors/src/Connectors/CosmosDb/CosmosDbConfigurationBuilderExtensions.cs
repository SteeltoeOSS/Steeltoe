// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;

namespace Steeltoe.Connectors.CosmosDb;

public static class CosmosDbConfigurationBuilderExtensions
{
    public static IConfigurationBuilder ConfigureCosmosDb(this IConfigurationBuilder builder)
    {
        return ConfigureCosmosDb(builder, null);
    }

    public static IConfigurationBuilder ConfigureCosmosDb(this IConfigurationBuilder builder, Action<ConnectorConfigureOptions>? configureAction)
    {
        ArgumentGuard.NotNull(builder);

        ConnectorConfigureOptions configureOptions = new();
        configureAction?.Invoke(configureOptions);

        RegisterPostProcessors(builder, configureOptions.DetectConfigurationChanges);
        return builder;
    }

    private static void RegisterPostProcessors(IConfigurationBuilder builder, bool detectConfigurationChanges)
    {
        builder.AddCloudFoundryServiceBindings();

        var connectionStringPostProcessor = new CosmosDbConnectionStringPostProcessor();
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
