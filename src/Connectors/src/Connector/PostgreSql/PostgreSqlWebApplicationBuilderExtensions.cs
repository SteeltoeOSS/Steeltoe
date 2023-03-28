// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Connector.PostgreSql;

public static class PostgreSqlWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddPostgreSql(this WebApplicationBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        var connectionStringPostProcessor = new PostgreSqlConnectionStringPostProcessor();
        Type connectionType = PostgreSqlTypeLocator.NpgsqlConnection;

        BaseWebApplicationBuilderExtensions.RegisterConfigurationSource(builder.Configuration, connectionStringPostProcessor);
        BaseWebApplicationBuilderExtensions.RegisterNamedOptions<PostgreSqlOptions>(builder, "postgresql", CreateHealthContributor);
        BaseWebApplicationBuilderExtensions.RegisterConnectionFactory<PostgreSqlOptions>(builder.Services, connectionType);

        return builder;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string bindingName)
    {
        return BaseWebApplicationBuilderExtensions.CreateRelationalHealthContributor<PostgreSqlOptions>(serviceProvider, bindingName,
            PostgreSqlTypeLocator.NpgsqlConnection, "PostgreSQL", "host");
    }
}
