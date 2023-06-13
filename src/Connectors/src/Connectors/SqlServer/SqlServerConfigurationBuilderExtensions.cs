// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Connectors.SqlServer.RuntimeTypeAccess;

namespace Steeltoe.Connectors.SqlServer;

public static class SqlServerConfigurationBuilderExtensions
{
    public static IConfigurationBuilder ConfigureSqlServer(this IConfigurationBuilder builder)
    {
        return ConfigureSqlServer(builder, null);
    }

    public static IConfigurationBuilder ConfigureSqlServer(this IConfigurationBuilder builder, Action<ConnectorConfigureOptionsBuilder>? configureAction)
    {
        return ConfigureSqlServer(builder, SqlServerPackageResolver.Default, configureAction);
    }

    internal static IConfigurationBuilder ConfigureSqlServer(this IConfigurationBuilder builder, SqlServerPackageResolver packageResolver,
        Action<ConnectorConfigureOptionsBuilder>? configureAction)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        ConnectorConfigurer.Configure(builder, configureAction, new SqlServerConnectionStringPostProcessor(packageResolver));
        return builder;
    }
}
