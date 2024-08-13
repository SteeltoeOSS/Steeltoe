// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Steeltoe.Connectors.DynamicTypeAccess;
using Steeltoe.Connectors.EntityFrameworkCore.MySql.DynamicTypeAccess;
using Steeltoe.Connectors.MySql;

namespace Steeltoe.Connectors.EntityFrameworkCore.MySql;

public static class MySqlDbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Configures the <see cref="DbContext" /> to connect to a MySQL compatible database, using the default service binding.
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
    public static DbContextOptionsBuilder UseMySql(this DbContextOptionsBuilder builder, IServiceProvider serviceProvider)
    {
        return UseMySql(builder, serviceProvider, MySqlEntityFrameworkCorePackageResolver.Default);
    }

    /// <summary>
    /// Configures the <see cref="DbContext" /> to connect to a MySQL compatible database, using a named service binding.
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
    public static DbContextOptionsBuilder UseMySql(this DbContextOptionsBuilder builder, IServiceProvider serviceProvider, string? serviceBindingName)
    {
        return UseMySql(builder, serviceProvider, MySqlEntityFrameworkCorePackageResolver.Default, serviceBindingName);
    }

    /// <summary>
    /// Configures the <see cref="DbContext" /> to connect to a MySQL compatible database, using a named service binding and options.
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
    /// <param name="serverVersion">
    /// The version of the database server. This only has an effect if the Pomelo.EntityFrameworkCore.MySql package is being used. Set to <c>null</c> to
    /// auto-detect (at the cost of opening an extra connection).
    /// </param>
    /// <param name="mySqlOptionsAction">
    /// An action to allow additional MySQL specific configuration.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static DbContextOptionsBuilder UseMySql(this DbContextOptionsBuilder builder, IServiceProvider serviceProvider, string? serviceBindingName,
        object? serverVersion, Action<object>? mySqlOptionsAction)
    {
        return UseMySql(builder, serviceProvider, MySqlEntityFrameworkCorePackageResolver.Default, serviceBindingName, serverVersion, mySqlOptionsAction);
    }

    internal static DbContextOptionsBuilder UseMySql(this DbContextOptionsBuilder builder, IServiceProvider serviceProvider,
        MySqlEntityFrameworkCorePackageResolver packageResolver, string? serviceBindingName = null, object? serverVersion = null,
        Action<object>? mySqlOptionsAction = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(packageResolver);

        string optionName = serviceBindingName ?? string.Empty;
        string? connectionString = GetConnectionString(serviceProvider, optionName, packageResolver);

        MySqlDbContextOptionsExtensionsShim.UseMySql(packageResolver, builder, connectionString, serverVersion, mySqlOptionsAction);

        return builder;
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
