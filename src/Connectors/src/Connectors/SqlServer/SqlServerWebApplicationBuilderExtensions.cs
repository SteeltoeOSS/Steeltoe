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
        return AddSqlServer(builder, null);
    }

    public static WebApplicationBuilder AddSqlServer(this WebApplicationBuilder builder, Action<ConnectorSetupOptions>? setupAction)
    {
        return AddSqlServer(builder, new SqlServerPackageResolver(), setupAction);
    }

    internal static WebApplicationBuilder AddSqlServer(this WebApplicationBuilder builder, SqlServerPackageResolver packageResolver,
        Action<ConnectorSetupOptions>? setupAction = null)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        builder.Services.AddSqlServer(builder.Configuration, packageResolver, setupAction);
        return builder;
    }
}
