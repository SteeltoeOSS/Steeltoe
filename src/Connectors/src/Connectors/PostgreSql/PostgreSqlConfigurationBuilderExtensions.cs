// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Connectors.PostgreSql.DynamicTypeAccess;

namespace Steeltoe.Connectors.PostgreSql;

public static class PostgreSqlConfigurationBuilderExtensions
{
    public static IConfigurationBuilder ConfigurePostgreSql(this IConfigurationBuilder builder)
    {
        return ConfigurePostgreSql(builder, null);
    }

    public static IConfigurationBuilder ConfigurePostgreSql(this IConfigurationBuilder builder, Action<ConnectorConfigureOptionsBuilder>? configureAction)
    {
        return ConfigurePostgreSql(builder, PostgreSqlPackageResolver.Default, configureAction);
    }

    private static IConfigurationBuilder ConfigurePostgreSql(this IConfigurationBuilder builder, PostgreSqlPackageResolver packageResolver,
        Action<ConnectorConfigureOptionsBuilder>? configureAction)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        ConnectorConfigurer.Configure(builder, configureAction, new PostgreSqlConnectionStringPostProcessor(packageResolver));
        return builder;
    }
}
