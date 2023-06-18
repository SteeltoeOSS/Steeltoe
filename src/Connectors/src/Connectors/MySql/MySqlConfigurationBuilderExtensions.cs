// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Connectors.MySql.DynamicTypeAccess;

namespace Steeltoe.Connectors.MySql;

public static class MySqlConfigurationBuilderExtensions
{
    public static IConfigurationBuilder ConfigureMySql(this IConfigurationBuilder builder)
    {
        return ConfigureMySql(builder, null);
    }

    public static IConfigurationBuilder ConfigureMySql(this IConfigurationBuilder builder, Action<ConnectorConfigureOptionsBuilder>? configureAction)
    {
        return ConfigureMySql(builder, MySqlPackageResolver.Default, configureAction);
    }

    internal static IConfigurationBuilder ConfigureMySql(this IConfigurationBuilder builder, MySqlPackageResolver packageResolver,
        Action<ConnectorConfigureOptionsBuilder>? configureAction)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        ConnectorConfigurer.Configure(builder, configureAction, new MySqlConnectionStringPostProcessor(packageResolver));
        return builder;
    }
}
