// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Steeltoe.Common;
using Steeltoe.Connectors.DynamicTypeAccess;
using Steeltoe.Connectors.EntityFrameworkCore.PostgreSql.DynamicTypeAccess;
using Steeltoe.Connectors.PostgreSql;

namespace Steeltoe.Connectors.EntityFrameworkCore.PostgreSql;

public static class PostgreSqlDbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Configures the context to connect to a PostgreSQL server with Npgsql, using the default service binding.
    /// </summary>
    /// <param name="optionsBuilder">
    /// The builder being used to configure the <see cref="DbContext" />.
    /// </param>
    /// <param name="serviceProvider">
    /// The application's configured services.
    /// </param>
    /// <returns>
    /// The <see cref="DbContextOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static DbContextOptionsBuilder UseNpgsql(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider)
    {
        return UseNpgsql(optionsBuilder, serviceProvider, PostgreSqlEntityFrameworkCorePackageResolver.Default);
    }

    /// <summary>
    /// Configures the context to connect to a PostgreSQL server with Npgsql, using a named service binding.
    /// </summary>
    /// <param name="optionsBuilder">
    /// The builder being used to configure the <see cref="DbContext" />.
    /// </param>
    /// <param name="serviceProvider">
    /// The application's configured services.
    /// </param>
    /// <param name="serviceBindingName">
    /// The service binding name, or <c>null</c> to use the default service binding.
    /// </param>
    /// <returns>
    /// The <see cref="DbContextOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static DbContextOptionsBuilder UseNpgsql(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider, string? serviceBindingName)
    {
        return UseNpgsql(optionsBuilder, serviceProvider, PostgreSqlEntityFrameworkCorePackageResolver.Default, serviceBindingName);
    }

    /// <summary>
    /// Configures the context to connect to a PostgreSQL server with Npgsql, using a named service binding and options.
    /// </summary>
    /// <param name="optionsBuilder">
    /// The builder being used to configure the <see cref="DbContext" />.
    /// </param>
    /// <param name="serviceProvider">
    /// The application's configured services.
    /// </param>
    /// <param name="serviceBindingName">
    /// The service binding name, or <c>null</c> to use the default service binding.
    /// </param>
    /// <param name="npgsqlOptionsAction">
    /// An action to allow additional Npgsql-specific configuration.
    /// </param>
    /// <returns>
    /// The <see cref="DbContextOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static DbContextOptionsBuilder UseNpgsql(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider, string? serviceBindingName,
        Action<object>? npgsqlOptionsAction)
    {
        return UseNpgsql(optionsBuilder, serviceProvider, PostgreSqlEntityFrameworkCorePackageResolver.Default, serviceBindingName, npgsqlOptionsAction);
    }

    private static DbContextOptionsBuilder UseNpgsql(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider,
        PostgreSqlEntityFrameworkCorePackageResolver packageResolver, string? serviceBindingName = null, Action<object>? npgsqlOptionsAction = null)
    {
        ArgumentGuard.NotNull(optionsBuilder);
        ArgumentGuard.NotNull(serviceProvider);
        ArgumentGuard.NotNull(packageResolver);

        string optionName = serviceBindingName ?? string.Empty;
        string? connectionString = GetConnectionString(serviceProvider, optionName, packageResolver);

        NpgsqlDbContextOptionsBuilderExtensionsShim.UseNpgsql(packageResolver, optionsBuilder, connectionString, npgsqlOptionsAction);

        return optionsBuilder;
    }

    private static string? GetConnectionString(IServiceProvider serviceProvider, string serviceBindingName,
        PostgreSqlEntityFrameworkCorePackageResolver packageResolver)
    {
        ConnectorFactoryShim<PostgreSqlOptions> connectorFactoryShim =
            ConnectorFactoryShim<PostgreSqlOptions>.FromServiceProvider(serviceProvider, packageResolver.NpgsqlConnectionClass.Type);

        ConnectorShim<PostgreSqlOptions> connectorShim = connectorFactoryShim.Get(serviceBindingName);
        return connectorShim.Options.ConnectionString;
    }
}
