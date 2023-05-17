// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.MySql.RuntimeTypeAccess;

namespace Steeltoe.Connectors.MySql;

public static class MySqlWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddMySql(this WebApplicationBuilder builder)
    {
        return AddMySql(builder, new MySqlPackageResolver());
    }

    internal static WebApplicationBuilder AddMySql(this WebApplicationBuilder builder, MySqlPackageResolver packageResolver)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        var connectionStringPostProcessor = new MySqlConnectionStringPostProcessor(packageResolver);

        BaseWebApplicationBuilderExtensions.RegisterConfigurationSource(builder.Configuration, connectionStringPostProcessor);

        BaseWebApplicationBuilderExtensions.RegisterNamedOptions<MySqlOptions>(builder, "mysql",
            (serviceProvider, bindingName) => CreateHealthContributor(serviceProvider, bindingName, packageResolver));

        BaseWebApplicationBuilderExtensions.RegisterConnectorFactory<MySqlOptions>(builder.Services, packageResolver.MySqlConnectionClass.Type, false, null);

        return builder;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string bindingName, MySqlPackageResolver packageResolver)
    {
        return BaseWebApplicationBuilderExtensions.CreateRelationalHealthContributor<MySqlOptions>(serviceProvider, bindingName,
            packageResolver.MySqlConnectionClass.Type, "MySQL", "server");
    }
}
