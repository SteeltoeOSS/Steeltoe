// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Connectors.SqlServer.RuntimeTypeAccess;

namespace Steeltoe.Connectors.SqlServer;

public static class SqlServerConfigurationBuilderExtensions
{
    public static IConfigurationBuilder ConfigureSqlServer(this IConfigurationBuilder builder)
    {
        return ConfigureSqlServer(builder, null);
    }

    public static IConfigurationBuilder ConfigureSqlServer(this IConfigurationBuilder builder, Action<ConnectorConfigureOptions>? configureAction)
    {
        return ConfigureSqlServer(builder, SqlServerPackageResolver.Default, configureAction);
    }

    internal static IConfigurationBuilder ConfigureSqlServer(this IConfigurationBuilder builder, SqlServerPackageResolver packageResolver,
        Action<ConnectorConfigureOptions>? configureAction)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        ConnectorConfigureOptions configureOptions = new();
        configureAction?.Invoke(configureOptions);

        RegisterPostProcessors(builder, packageResolver, configureOptions.DetectConfigurationChanges);
        return builder;
    }

    private static void RegisterPostProcessors(IConfigurationBuilder builder, SqlServerPackageResolver packageResolver, bool detectConfigurationChanges)
    {
        builder.AddCloudFoundryServiceBindings();

        var connectionStringPostProcessor = new SqlServerConnectionStringPostProcessor(packageResolver);
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
