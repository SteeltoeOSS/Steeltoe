// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Connector.MySql;

namespace Steeltoe.Connector.EntityFrameworkCore.MySql;

public static class MySqlDbContextOptionsBuilderExtensions
{
    private const string BindingType = "mysql";
    private static readonly string ServiceBindingsConfigurationKey = ConfigurationPath.Combine("steeltoe", "service-bindings");

    public static DbContextOptionsBuilder UseMySql(this DbContextOptionsBuilder optionsBuilder, IConfigurationBuilder configurationBuilder,
        string serviceBindingName = null, Action<object> mySqlOptionsAction = null)
    {
        ArgumentGuard.NotNull(optionsBuilder);
        ArgumentGuard.NotNull(configurationBuilder);

        string configurationKey = ConfigurationPath.Combine(ServiceBindingsConfigurationKey, BindingType, serviceBindingName ?? "Default", "ConnectionString");
        IConfigurationRoot configurationRoot = configurationBuilder.Build();
        string connectionString = configurationRoot[configurationKey];

        if (connectionString == null)
        {
            throw new InvalidOperationException($"Connection string for service binding '{serviceBindingName}' not found. " +
                $"Please verify that you have called {nameof(MySqlWebApplicationBuilderExtensions.AddMySql)}() first.");
        }

        MySqlDbContextOptionsExtensions.DoUseMySql(optionsBuilder, connectionString, mySqlOptionsAction);

        return optionsBuilder;
    }
}
