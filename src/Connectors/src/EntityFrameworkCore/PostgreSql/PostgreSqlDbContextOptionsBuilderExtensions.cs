// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.EntityFrameworkCore;
using Steeltoe.Common;
using Steeltoe.Connectors.EntityFrameworkCore.PostgreSql.RuntimeTypeAccess;
using Steeltoe.Connectors.PostgreSql;

namespace Steeltoe.Connectors.EntityFrameworkCore.PostgreSql;

public static class PostgreSqlDbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseNpgsql(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider,
        string? serviceBindingName = null, Action<object>? npgsqlOptionsAction = null)
    {
        return UseNpgsql(optionsBuilder, serviceProvider, new PostgreSqlEntityFrameworkCorePackageResolver(), serviceBindingName, npgsqlOptionsAction);
    }

    private static DbContextOptionsBuilder UseNpgsql(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider,
        PostgreSqlEntityFrameworkCorePackageResolver packageResolver, string? serviceBindingName, Action<object>? npgsqlOptionsAction)
    {
        ArgumentGuard.NotNull(optionsBuilder);
        ArgumentGuard.NotNull(serviceProvider);
        ArgumentGuard.NotNull(packageResolver);

        string optionName = serviceBindingName ?? string.Empty;

        string connectionString =
            ConnectorFactoryInvoker.GetConnectionString<PostgreSqlOptions>(serviceProvider, optionName, packageResolver.NpgsqlConnectionClass.Type);

        if (connectionString == null)
        {
            throw new InvalidOperationException($"Connection string for service binding '{serviceBindingName}' not found.");
        }

        NpgsqlDbContextOptionsBuilderExtensionsShim.UseNpgsql(packageResolver, optionsBuilder, connectionString, npgsqlOptionsAction);

        return optionsBuilder;
    }
}
