// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Connectors.SqlServer;

public static class SqlServerWebApplicationBuilderExtensions
{
    private static readonly Type ConnectionType = SqlServerTypeLocator.SqlConnection;

    public static WebApplicationBuilder AddSqlServer(this WebApplicationBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        var connectionStringPostProcessor = new SqlServerConnectionStringPostProcessor();

        BaseWebApplicationBuilderExtensions.RegisterConfigurationSource(builder.Configuration, connectionStringPostProcessor);
        BaseWebApplicationBuilderExtensions.RegisterNamedOptions<SqlServerOptions>(builder, "sqlserver", CreateHealthContributor);
        BaseWebApplicationBuilderExtensions.RegisterConnectorFactory<SqlServerOptions>(builder.Services, ConnectionType, false, null);

        return builder;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string bindingName)
    {
        return BaseWebApplicationBuilderExtensions.CreateRelationalHealthContributor<SqlServerOptions>(serviceProvider, bindingName, ConnectionType,
            "SqlServer", "Data Source");
    }
}
