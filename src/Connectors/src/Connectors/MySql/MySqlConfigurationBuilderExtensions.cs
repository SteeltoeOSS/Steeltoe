// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Configuration.Kubernetes.ServiceBinding;
using Steeltoe.Connectors.MySql.DynamicTypeAccess;

namespace Steeltoe.Connectors.MySql;

public static class MySqlConfigurationBuilderExtensions
{
    public static IConfigurationBuilder ConfigureMySql(this IConfigurationBuilder builder)
    {
        return ConfigureMySql(builder, null);
    }

    public static IConfigurationBuilder ConfigureMySql(this IConfigurationBuilder builder, Action<ConnectorConfigureOptions>? configureAction)
    {
        return ConfigureMySql(builder, MySqlPackageResolver.Default, configureAction);
    }

    internal static IConfigurationBuilder ConfigureMySql(this IConfigurationBuilder builder, MySqlPackageResolver packageResolver,
        Action<ConnectorConfigureOptions>? configureAction)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        if (!IsConfigured(builder))
        {
            var configureOptions = new ConnectorConfigureOptions();
            configureAction?.Invoke(configureOptions);

            RegisterPostProcessors(builder, packageResolver, configureOptions.DetectConfigurationChanges);
        }

        return builder;
    }

    private static bool IsConfigured(IConfigurationBuilder builder)
    {
        return builder.Sources.OfType<ConnectionStringPostProcessorConfigurationSource>().Any(connectionStringSource =>
            connectionStringSource.PostProcessors.Any(postProcessor => postProcessor is MySqlConnectionStringPostProcessor));
    }

    private static void RegisterPostProcessors(IConfigurationBuilder builder, MySqlPackageResolver packageResolver, bool detectConfigurationChanges)
    {
        builder.AddCloudFoundryServiceBindings();
        builder.AddKubernetesServiceBindings();

        var connectionStringPostProcessor = new MySqlConnectionStringPostProcessor(packageResolver);
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
