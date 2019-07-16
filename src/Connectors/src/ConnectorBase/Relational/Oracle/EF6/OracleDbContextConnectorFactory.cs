// Copyright 2019 Infosys Ltd.
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

using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.Connector.Services;
using System;

namespace Steeltoe.CloudFoundry.ConnectorBase.Relational.Oracle.EF6
{
    public class OracleDbContextConnectorFactory : OracleProviderConnectorFactory
    {
        public OracleDbContextConnectorFactory(OracleServiceInfo info, OracleProviderConnectorOptions config, Type dbContextType)
    : base(info, config, dbContextType)
        {
            if (dbContextType == null)
            {
                throw new ArgumentNullException(nameof(dbContextType));
            }
        }

        internal OracleDbContextConnectorFactory()
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
