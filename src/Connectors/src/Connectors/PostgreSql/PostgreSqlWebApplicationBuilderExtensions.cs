// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common;

namespace Steeltoe.Connectors.PostgreSql;

public static class PostgreSqlWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddPostgreSql(this WebApplicationBuilder builder)
    {
        return AddPostgreSql(builder, null, null);
    }

    public static WebApplicationBuilder AddPostgreSql(this WebApplicationBuilder builder, Action<ConnectorConfigureOptionsBuilder>? configureAction,
        Action<ConnectorAddOptionsBuilder>? addAction)
    {
        ArgumentGuard.NotNull(builder);

        builder.Configuration.ConfigurePostgreSql(configureAction);
        builder.Services.AddPostgreSql(builder.Configuration, addAction);
        return builder;
    }
}
