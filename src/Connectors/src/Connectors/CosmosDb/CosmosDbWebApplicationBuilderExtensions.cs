// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common;

namespace Steeltoe.Connectors.CosmosDb;

public static class CosmosDbWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddCosmosDb(this WebApplicationBuilder builder)
    {
        return AddCosmosDb(builder, null, null);
    }

    public static WebApplicationBuilder AddCosmosDb(this WebApplicationBuilder builder, Action<ConnectorConfigureOptionsBuilder>? configureAction,
        Action<ConnectorAddOptionsBuilder>? addAction)
    {
        ArgumentGuard.NotNull(builder);

        builder.Configuration.ConfigureCosmosDb(configureAction);
        builder.Services.AddCosmosDb(builder.Configuration, addAction);
        return builder;
    }
}
