// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.EntityFrameworkCore;
using Steeltoe.Common;
using Steeltoe.Connectors.EntityFrameworkCore.SqlServer.RuntimeTypeAccess;
using Steeltoe.Connectors.RuntimeTypeAccess;
using Steeltoe.Connectors.SqlServer;

namespace Steeltoe.Connectors.EntityFrameworkCore.SqlServer;

public static class SqlServerDbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseSqlServer(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider,
        string? serviceBindingName = null, Action<object>? sqlServerOptionsAction = null)
    {
        return UseSqlServer(optionsBuilder, serviceProvider, new SqlServerEntityFrameworkCorePackageResolver(), serviceBindingName, sqlServerOptionsAction);
    }

    private static DbContextOptionsBuilder UseSqlServer(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider,
        SqlServerEntityFrameworkCorePackageResolver packageResolver, string? serviceBindingName = null, Action<object>? sqlServerOptionsAction = null)
    {
        ArgumentGuard.NotNull(optionsBuilder);
        ArgumentGuard.NotNull(serviceProvider);
        ArgumentGuard.NotNull(packageResolver);

        string optionName = serviceBindingName ?? string.Empty;
        string? connectionString = GetConnectionString(serviceProvider, optionName, packageResolver);

        SqlServerDbContextOptionsExtensionsShim.UseSqlServer(packageResolver, optionsBuilder, connectionString, sqlServerOptionsAction);

        return optionsBuilder;
    }

    private static string? GetConnectionString(IServiceProvider serviceProvider, string serviceBindingName,
        SqlServerEntityFrameworkCorePackageResolver packageResolver)
    {
        ConnectorFactoryShim<SqlServerOptions> connectorFactoryShim =
            ConnectorFactoryShim<SqlServerOptions>.FromServiceProvider(serviceProvider, packageResolver.SqlConnectionClass.Type);

        ConnectorShim<SqlServerOptions> connectorShim = connectorFactoryShim.GetNamed(serviceBindingName);
        return connectorShim.Options.ConnectionString;
    }
}
