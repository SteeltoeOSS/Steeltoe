// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Connector.SqlServer;

public static class SqlServerWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddSqlServer(this WebApplicationBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        var connectionStringPostProcessor = new SqlServerConnectionStringPostProcessor();
        Type connectionType = SqlServerTypeLocator.SqlConnection;

        BaseWebApplicationBuilderExtensions.RegisterConfigurationSource(builder.Configuration, connectionStringPostProcessor);
        BaseWebApplicationBuilderExtensions.RegisterNamedOptions<SqlServerOptions>(builder, "sqlserver", CreateHealthContributor);
        BaseWebApplicationBuilderExtensions.RegisterConnectionFactory<SqlServerOptions>(builder.Services, connectionType);

        return builder;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string bindingName)
    {
        return BaseWebApplicationBuilderExtensions.CreateRelationalHealthContributor<SqlServerOptions>(serviceProvider, bindingName,
            SqlServerTypeLocator.SqlConnection, "SqlServer", "Data Source");
    }
}
