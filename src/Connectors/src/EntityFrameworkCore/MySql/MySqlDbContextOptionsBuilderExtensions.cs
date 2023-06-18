// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Steeltoe.Common;
using Steeltoe.Connector.MySql;

namespace Steeltoe.Connector.EntityFrameworkCore.MySql;

public static class MySqlDbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseMySql(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider,
        string serviceBindingName = null, object serverVersion = null, Action<object> mySqlOptionsAction = null)
    {
        ArgumentGuard.NotNull(optionsBuilder);
        ArgumentGuard.NotNull(serviceProvider);

        Type connectionType = MySqlTypeLocator.MySqlConnection;

        string optionName = serviceBindingName ?? string.Empty;
        string connectionString = ConnectionFactoryInvoker.GetConnectionString<MySqlOptions>(serviceProvider, optionName, connectionType);

        if (connectionString == null)
        {
            throw new InvalidOperationException($"Connection string for service binding '{serviceBindingName}' not found.");
        }

        MySqlDbContextOptionsExtensions.DoUseMySql(optionsBuilder, connectionString, mySqlOptionsAction, serverVersion);

        return optionsBuilder;
    }
}
