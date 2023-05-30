// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common;
using Steeltoe.Connectors.MySql.RuntimeTypeAccess;

namespace Steeltoe.Connectors.MySql;

public static class MySqlWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddMySql(this WebApplicationBuilder builder)
    {
        return AddMySql(builder, null);
    }

    public static WebApplicationBuilder AddMySql(this WebApplicationBuilder builder, Action<ConnectorSetupOptions>? setupAction)
    {
        return AddMySql(builder, new MySqlPackageResolver(), setupAction);
    }

    internal static WebApplicationBuilder AddMySql(this WebApplicationBuilder builder, MySqlPackageResolver packageResolver,
        Action<ConnectorSetupOptions>? setupAction = null)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        builder.Services.AddMySql(builder.Configuration, packageResolver, setupAction);
        return builder;
    }
}
