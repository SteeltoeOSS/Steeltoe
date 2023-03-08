// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common;

namespace Steeltoe.Connector.MySql;

public static class MySqlWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddMySql(this WebApplicationBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        var connectionStringPostProcessor = new MySqlConnectionStringPostProcessor();
        Type connectionType = MySqlTypeLocator.MySqlConnection;

        BaseWebApplicationBuilderExtensions.RegisterConfigurationSource(builder.Configuration, connectionStringPostProcessor);
        BaseWebApplicationBuilderExtensions.RegisterNamedOptions<MySqlOptions>(builder, "mysql", connectionType, "MySQL", "server");
        BaseWebApplicationBuilderExtensions.RegisterConnectionFactory<MySqlOptions>(builder.Services, connectionType);

        return builder;
    }
}
