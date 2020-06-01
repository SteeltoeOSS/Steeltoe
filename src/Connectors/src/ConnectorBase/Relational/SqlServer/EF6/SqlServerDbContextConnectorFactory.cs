// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CloudFoundry.Connector.Services;
using System;

namespace Steeltoe.CloudFoundry.Connector.SqlServer.EF6
{
    public class SqlServerDbContextConnectorFactory : SqlServerProviderConnectorFactory
    {
        public SqlServerDbContextConnectorFactory(SqlServerServiceInfo info, SqlServerProviderConnectorOptions config, Type dbContextType)
            : base(info, config, dbContextType)
        {
            if (dbContextType == null)
            {
                throw new ArgumentNullException(nameof(dbContextType));
            }
        }

        internal SqlServerDbContextConnectorFactory()
        {
        }

        public override object Create(IServiceProvider arg)
        {
            var connectionString = CreateConnectionString();
            object result = null;
            if (connectionString != null)
            {
                result = ConnectorHelpers.CreateInstance(ConnectorType, new object[] { connectionString });
            }

            if (result == null)
            {
                throw new ConnectorException(string.Format("Unable to create instance of '{0}', are you missing 'public {0}(string connectionString)' constructor", ConnectorType));
            }

            return result;
        }
    }
}