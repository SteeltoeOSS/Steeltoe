// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Configuration.Kubernetes.ServiceBinding;
using Steeltoe.Connectors.PostgreSql.DynamicTypeAccess;

namespace Steeltoe.Connectors.PostgreSql;

public static class PostgreSqlConfigurationBuilderExtensions
{
    public static IConfigurationBuilder ConfigurePostgreSql(this IConfigurationBuilder builder)
    {
        return ConfigurePostgreSql(builder, null);
    }

    public static IConfigurationBuilder ConfigurePostgreSql(this IConfigurationBuilder builder, Action<ConnectorConfigureOptions>? configureAction)
    {
        return ConfigurePostgreSql(builder, PostgreSqlPackageResolver.Default, configureAction);
    }

    private static IConfigurationBuilder ConfigurePostgreSql(this IConfigurationBuilder builder, PostgreSqlPackageResolver packageResolver,
        Action<ConnectorConfigureOptions>? configureAction)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        if (!IsConfigured(builder))
        {
            ConnectorConfigureOptions configureOptions = new();
            configureAction?.Invoke(configureOptions);

            RegisterPostProcessors(builder, packageResolver, configureOptions.DetectConfigurationChanges);
        }

        return builder;
    }

    private static bool IsConfigured(IConfigurationBuilder builder)
    {
        return builder.Sources.OfType<ConnectionStringPostProcessorConfigurationSource>().Any(connectionStringSource =>
            connectionStringSource.PostProcessors.Any(postProcessor => postProcessor is PostgreSqlConnectionStringPostProcessor));
    }

    private static void RegisterPostProcessors(IConfigurationBuilder builder, PostgreSqlPackageResolver packageResolver, bool detectConfigurationChanges)
    {
        builder.AddCloudFoundryServiceBindings();
        builder.AddKubernetesServiceBindings();

        var connectionStringPostProcessor = new PostgreSqlConnectionStringPostProcessor(packageResolver);
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
