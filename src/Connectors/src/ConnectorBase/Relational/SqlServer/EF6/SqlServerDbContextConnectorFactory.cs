// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;
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
                result = ReflectionHelpers.CreateInstance(ConnectorType, new object[] { connectionString });
            }

            if (result == null)
            {
                throw new ConnectorException(string.Format("Unable to create instance of '{0}', are you missing 'public {0}(string connectionString)' constructor", ConnectorType));
            }

            return result;
        }
    }
}