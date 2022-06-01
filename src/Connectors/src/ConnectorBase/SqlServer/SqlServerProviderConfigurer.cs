// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Extensions;
using Steeltoe.Connector.Services;
using System;

namespace Steeltoe.Connector.SqlServer;

public class SqlServerProviderConfigurer
{
    public string Configure(SqlServerServiceInfo si, SqlServerProviderConnectorOptions configuration)
    {
        UpdateConfiguration(si, configuration);
        return configuration.ToString();
    }

    public void UpdateConfiguration(SqlServerServiceInfo si, SqlServerProviderConnectorOptions configuration)
    {
        if (si == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(si.Uri))
        {
            configuration.Port = si.Port;
            configuration.Server = si.Host;
            if (!string.IsNullOrEmpty(si.Path))
            {
                configuration.Database = si.Path;
            }

            if (si.Query != null)
            {
                foreach (var kvp in UriExtensions.ParseQuerystring(si.Query))
                {
                    if (kvp.Key.EndsWith("database", StringComparison.InvariantCultureIgnoreCase) || kvp.Key.EndsWith("databaseName", StringComparison.InvariantCultureIgnoreCase))
                    {
                        configuration.Database = kvp.Value;
                    }
                    else if (kvp.Key.EndsWith("instancename", StringComparison.InvariantCultureIgnoreCase))
                    {
                        configuration.InstanceName = kvp.Value;
                    }
                    else if (kvp.Key.StartsWith("hostnameincertificate", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // adding this key could result in "System.ArgumentException : Keyword not supported: 'hostnameincertificate'" later
                    }
                    else
                    {
                        configuration.Options.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            configuration.Username = si.UserName;
            configuration.Password = si.Password;
        }
    }
}
