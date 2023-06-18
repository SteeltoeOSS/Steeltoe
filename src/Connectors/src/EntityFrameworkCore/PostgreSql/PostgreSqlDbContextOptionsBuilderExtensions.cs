// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Steeltoe.Common;
using Steeltoe.Connector.PostgreSql;

namespace Steeltoe.Connector.EntityFrameworkCore.PostgreSql;

public static class PostgreSqlDbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseNpgsql(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider,
        string serviceBindingName = null, Action<object> npgsqlOptionsAction = null)
    {
        ArgumentGuard.NotNull(optionsBuilder);
        ArgumentGuard.NotNull(serviceProvider);

        Type connectionType = PostgreSqlTypeLocator.NpgsqlConnection;

        string optionName = serviceBindingName ?? string.Empty;
        string connectionString = ConnectionFactoryInvoker.GetConnectionString<PostgreSqlOptions>(serviceProvider, optionName, connectionType);

        if (connectionString == null)
        {
            throw new InvalidOperationException($"Connection string for service binding '{serviceBindingName}' not found.");
        }

        PostgreSqlDbContextOptionsExtensions.DoUseNpgsql(optionsBuilder, connectionString, npgsqlOptionsAction);

        return optionsBuilder;
    }
}
