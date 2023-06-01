// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.EntityFrameworkCore;
using Steeltoe.Common;
using Steeltoe.Connectors.DynamicTypeAccess;
using Steeltoe.Connectors.EntityFrameworkCore.MySql.DynamicTypeAccess;
using Steeltoe.Connectors.MySql;

namespace Steeltoe.Connectors.EntityFrameworkCore.MySql;

public static class MySqlDbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseMySql(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider,
        string? serviceBindingName = null, object? serverVersion = null, Action<object>? mySqlOptionsAction = null)
    {
        return UseMySql(optionsBuilder, serviceProvider, MySqlEntityFrameworkCorePackageResolver.Default, serviceBindingName, serverVersion,
            mySqlOptionsAction);
    }

    internal static DbContextOptionsBuilder UseMySql(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider,
        MySqlEntityFrameworkCorePackageResolver packageResolver, string? serviceBindingName = null, object? serverVersion = null,
        Action<object>? mySqlOptionsAction = null)
    {
        ArgumentGuard.NotNull(optionsBuilder);
        ArgumentGuard.NotNull(serviceProvider);
        ArgumentGuard.NotNull(packageResolver);

        string optionName = serviceBindingName ?? string.Empty;
        string? connectionString = GetConnectionString(serviceProvider, optionName, packageResolver);

        MySqlDbContextOptionsExtensionsShim.UseMySql(packageResolver, optionsBuilder, connectionString, serverVersion, mySqlOptionsAction);

        return optionsBuilder;
    }

    private static string? GetConnectionString(IServiceProvider serviceProvider, string serviceBindingName,
        MySqlEntityFrameworkCorePackageResolver packageResolver)
    {
        ConnectorFactoryShim<MySqlOptions> connectorFactoryShim =
            ConnectorFactoryShim<MySqlOptions>.FromServiceProvider(serviceProvider, packageResolver.MySqlConnectionClass.Type);

        ConnectorShim<MySqlOptions> connectorShim = connectorFactoryShim.Get(serviceBindingName);
        return connectorShim.Options.ConnectionString;
    }
}
