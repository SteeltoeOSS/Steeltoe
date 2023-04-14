// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Steeltoe.Common;
using Steeltoe.Connector.SqlServer;

namespace Steeltoe.Connector.EntityFrameworkCore.SqlServer;

public static class SqlServerDbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseSqlServer(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider,
        string serviceBindingName = null, Action<object> sqlServerOptionsAction = null)
    {
        ArgumentGuard.NotNull(optionsBuilder);
        ArgumentGuard.NotNull(serviceProvider);

        Type connectionType = SqlServerTypeLocator.SqlConnection;

        string optionName = serviceBindingName ?? string.Empty;
        string connectionString = ConnectionFactoryInvoker.GetConnectionString<SqlServerOptions>(serviceProvider, optionName, connectionType);

        if (connectionString == null)
        {
            throw new InvalidOperationException($"Connection string for service binding '{serviceBindingName}' not found.");
        }

        SqlServerDbContextOptionsExtensions.DoUseSqlServer(optionsBuilder, connectionString, sqlServerOptionsAction);

        return optionsBuilder;
    }
}
