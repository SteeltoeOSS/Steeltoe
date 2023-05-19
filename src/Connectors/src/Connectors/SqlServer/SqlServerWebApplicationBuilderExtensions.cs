// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.SqlServer.RuntimeTypeAccess;

namespace Steeltoe.Connectors.SqlServer;

public static class SqlServerWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddSqlServer(this WebApplicationBuilder builder)
    {
        return AddSqlServer(builder, new SqlServerPackageResolver());
    }

    internal static WebApplicationBuilder AddSqlServer(this WebApplicationBuilder builder, SqlServerPackageResolver packageResolver)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        var connectionStringPostProcessor = new SqlServerConnectionStringPostProcessor(packageResolver);

        BaseWebApplicationBuilderExtensions.RegisterConfigurationSource(builder.Configuration, connectionStringPostProcessor);

        BaseWebApplicationBuilderExtensions.RegisterNamedOptions<SqlServerOptions>(builder, "sqlserver",
            (serviceProvider, bindingName) => CreateHealthContributor(serviceProvider, bindingName, packageResolver));

        BaseWebApplicationBuilderExtensions.RegisterConnectorFactory<SqlServerOptions>(builder.Services, packageResolver.SqlConnectionClass.Type, false, null);

        return builder;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string bindingName, SqlServerPackageResolver packageResolver)
    {
        return BaseWebApplicationBuilderExtensions.CreateRelationalHealthContributor<SqlServerOptions>(serviceProvider, bindingName,
            packageResolver.SqlConnectionClass.Type, "SqlServer", "Data Source");
    }
}
