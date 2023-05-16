// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using Steeltoe.Common.Reflection;
using Steeltoe.Connectors.Services;

namespace Steeltoe.Connectors.MySql.EntityFramework6;

public class MySqlDbContextConnectorFactory : MySqlProviderConnectorFactory
{
    public MySqlDbContextConnectorFactory(MySqlServiceInfo info, MySqlProviderConnectorOptions options, Type dbContextType)
        : base(info, options, dbContextType)
    {
        ArgumentGuard.NotNull(dbContextType);
    }

    internal MySqlDbContextConnectorFactory()
    {
    }

    public override object Create(IServiceProvider provider)
    {
        string connectionString = CreateConnectionString();
        object result = null;

        if (connectionString != null)
        {
            result = ReflectionHelpers.CreateInstance(ConnectorType, new object[]
            {
                connectionString
            });
        }

        if (result == null)
        {
            throw new ConnectorException(
                $"Unable to create instance of '{ConnectorType}', are you missing 'public {ConnectorType}(string connectionString)' constructor");
        }

        return result;
    }
}
