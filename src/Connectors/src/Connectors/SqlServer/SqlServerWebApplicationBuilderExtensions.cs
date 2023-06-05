// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common;
using Steeltoe.Connectors.SqlServer.RuntimeTypeAccess;

namespace Steeltoe.Connectors.SqlServer;

public static class SqlServerWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddSqlServer(this WebApplicationBuilder builder)
    {
        return AddSqlServer(builder, null, null);
    }

    public static WebApplicationBuilder AddSqlServer(this WebApplicationBuilder builder, Action<ConnectorConfigureOptions>? configureAction,
        Action<ConnectorAddOptions>? addAction)
    {
        return AddSqlServer(builder, SqlServerPackageResolver.Default, configureAction, addAction);
    }

    internal static WebApplicationBuilder AddSqlServer(this WebApplicationBuilder builder, SqlServerPackageResolver packageResolver,
        Action<ConnectorConfigureOptions>? configureAction, Action<ConnectorAddOptions>? addAction)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        builder.Configuration.ConfigureSqlServer(packageResolver, configureAction);
        builder.Services.AddSqlServer(builder.Configuration, packageResolver, addAction);
        return builder;
    }
}
