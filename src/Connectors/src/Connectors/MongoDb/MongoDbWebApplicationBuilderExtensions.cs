// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common;

namespace Steeltoe.Connectors.MongoDb;

public static class MongoDbWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddMongoDb(this WebApplicationBuilder builder)
    {
        return AddMongoDb(builder, null, null);
    }

    public static WebApplicationBuilder AddMongoDb(this WebApplicationBuilder builder, Action<ConnectorConfigureOptions>? configureAction,
        Action<ConnectorAddOptions>? addAction)
    {
        ArgumentGuard.NotNull(builder);

        builder.Configuration.ConfigureMongoDb(configureAction);
        builder.Services.AddMongoDb(builder.Configuration, addAction);
        return builder;
    }
}
