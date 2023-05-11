// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.PostgreSql.RuntimeTypeAccess;

namespace Steeltoe.Connectors.PostgreSql;

public static class PostgreSqlWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddPostgreSql(this WebApplicationBuilder builder)
    {
        return AddPostgreSql(builder, new PostgreSqlPackageResolver());
    }

    private static WebApplicationBuilder AddPostgreSql(this WebApplicationBuilder builder, PostgreSqlPackageResolver packageResolver)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        var connectionStringPostProcessor = new PostgreSqlConnectionStringPostProcessor(packageResolver);

        BaseWebApplicationBuilderExtensions.RegisterConfigurationSource(builder.Configuration, connectionStringPostProcessor);

        BaseWebApplicationBuilderExtensions.RegisterNamedOptions<PostgreSqlOptions>(builder, "postgresql",
            (serviceProvider, bindingName) => CreateHealthContributor(serviceProvider, bindingName, packageResolver));

        BaseWebApplicationBuilderExtensions.RegisterConnectorFactory<PostgreSqlOptions>(builder.Services, packageResolver.NpgsqlConnectionClass.Type, false,
            null);

        return builder;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string bindingName, PostgreSqlPackageResolver packageResolver)
    {
        return BaseWebApplicationBuilderExtensions.CreateRelationalHealthContributor<PostgreSqlOptions>(serviceProvider, bindingName,
            packageResolver.NpgsqlConnectionClass.Type, "PostgreSQL", "host");
    }
}
