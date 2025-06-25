// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Steeltoe.Connectors.DynamicTypeAccess;
using Steeltoe.Connectors.EntityFrameworkCore.SqlServer.DynamicTypeAccess;
using Steeltoe.Connectors.SqlServer;

namespace Steeltoe.Connectors.EntityFrameworkCore.SqlServer;

public static class SqlServerDbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Configures the <see cref="DbContext" /> to connect to a Microsoft SQL Server database, using the default service binding.
    /// </summary>
    /// <param name="builder">
    /// The builder being used to configure the <see cref="DbContext" />.
    /// </param>
    /// <param name="serviceProvider">
    /// The application's configured services.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static DbContextOptionsBuilder UseSqlServer(this DbContextOptionsBuilder builder, IServiceProvider serviceProvider)
    {
        return UseSqlServer(builder, serviceProvider, SqlServerEntityFrameworkCorePackageResolver.Default);
    }

    /// <summary>
    /// Configures the <see cref="DbContext" /> to connect to a Microsoft SQL Server database, using a named service binding.
    /// </summary>
    /// <param name="builder">
    /// The builder being used to configure the <see cref="DbContext" />.
    /// </param>
    /// <param name="serviceProvider">
    /// The application's configured services.
    /// </param>
    /// <param name="serviceBindingName">
    /// The service binding name, or <c>null</c> to use the default service binding.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static DbContextOptionsBuilder UseSqlServer(this DbContextOptionsBuilder builder, IServiceProvider serviceProvider, string? serviceBindingName)
    {
        return UseSqlServer(builder, serviceProvider, SqlServerEntityFrameworkCorePackageResolver.Default, serviceBindingName);
    }

    /// <summary>
    /// Configures the <see cref="DbContext" /> to connect to a Microsoft SQL Server database, using a named service binding and options.
    /// </summary>
    /// <param name="builder">
    /// The builder being used to configure the <see cref="DbContext" />.
    /// </param>
    /// <param name="serviceProvider">
    /// The application's configured services.
    /// </param>
    /// <param name="serviceBindingName">
    /// The service binding name, or <c>null</c> to use the default service binding.
    /// </param>
    /// <param name="sqlServerOptionsAction">
    /// An action to allow additional SQL Server specific configuration.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static DbContextOptionsBuilder UseSqlServer(this DbContextOptionsBuilder builder, IServiceProvider serviceProvider, string? serviceBindingName,
        Action<object>? sqlServerOptionsAction)
    {
        return UseSqlServer(builder, serviceProvider, SqlServerEntityFrameworkCorePackageResolver.Default, serviceBindingName, sqlServerOptionsAction);
    }

    private static DbContextOptionsBuilder UseSqlServer(this DbContextOptionsBuilder builder, IServiceProvider serviceProvider,
        SqlServerEntityFrameworkCorePackageResolver packageResolver, string? serviceBindingName = null, Action<object>? sqlServerOptionsAction = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(packageResolver);

        string optionName = serviceBindingName ?? string.Empty;
        string? connectionString = GetConnectionString(serviceProvider, optionName, packageResolver);

        SqlServerDbContextOptionsExtensionsShim.UseSqlServer(packageResolver, builder, connectionString, sqlServerOptionsAction);

        return builder;
    }

    private static string? GetConnectionString(IServiceProvider serviceProvider, string serviceBindingName,
        SqlServerEntityFrameworkCorePackageResolver packageResolver)
    {
        ConnectorFactoryShim<SqlServerOptions> connectorFactoryShim =
            ConnectorFactoryShim<SqlServerOptions>.FromServiceProvider(serviceProvider, packageResolver.SqlConnectionClass.Type);

        ConnectorShim<SqlServerOptions> connectorShim = connectorFactoryShim.Get(serviceBindingName);
        return connectorShim.Options.ConnectionString;
    }
}
