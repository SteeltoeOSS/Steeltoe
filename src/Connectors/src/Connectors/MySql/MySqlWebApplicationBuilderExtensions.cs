// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common;
using Steeltoe.Connectors.MySql.DynamicTypeAccess;

namespace Steeltoe.Connectors.MySql;

public static class MySqlWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddMySql(this WebApplicationBuilder builder)
    {
        return AddMySql(builder, null, null);
    }

    public static WebApplicationBuilder AddMySql(this WebApplicationBuilder builder, Action<ConnectorConfigureOptionsBuilder>? configureAction,
        Action<ConnectorAddOptionsBuilder>? addAction)
    {
        return AddMySql(builder, MySqlPackageResolver.Default, configureAction, addAction);
    }

    internal static WebApplicationBuilder AddMySql(this WebApplicationBuilder builder, MySqlPackageResolver packageResolver,
        Action<ConnectorConfigureOptionsBuilder>? configureAction, Action<ConnectorAddOptionsBuilder>? addAction)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        builder.Configuration.ConfigureMySql(packageResolver, configureAction);
        builder.Services.AddMySql(builder.Configuration, packageResolver, addAction);
        return builder;
    }
}
