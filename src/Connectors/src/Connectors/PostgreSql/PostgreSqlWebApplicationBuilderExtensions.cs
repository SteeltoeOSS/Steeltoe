// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common;

namespace Steeltoe.Connectors.PostgreSql;

public static class PostgreSqlWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddPostgreSql(this WebApplicationBuilder builder)
    {
        return AddPostgreSql(builder, null);
    }

    public static WebApplicationBuilder AddPostgreSql(this WebApplicationBuilder builder, Action<ConnectorSetupOptions>? setupAction)
    {
        ArgumentGuard.NotNull(builder);

        builder.Configuration.ConfigurePostgreSql();
        builder.Services.AddPostgreSql(builder.Configuration, setupAction);
        return builder;
    }
}
