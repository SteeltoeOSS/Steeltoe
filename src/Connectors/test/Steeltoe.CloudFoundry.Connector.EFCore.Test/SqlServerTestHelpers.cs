// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Steeltoe.CloudFoundry.Connector.SqlServer.EFCore.Test
{
    public class SqlServerTestHelpers
    {
        public static string SingleServerVCAP = @"
                        {
                            'SqlServer': [
                                {
                                    'credentials': {
                                        'uid': 'uf33b2b30783a4087948c30f6c3b0c90f',
                                        'uri': 'jdbc:sqlserver://192.168.0.80:1433;databaseName=de5aa3a747c134b3d8780f8cc80be519e',
                                        'db': 'de5aa3a747c134b3d8780f8cc80be519e',
                                        'pw': 'Pefbb929c1e0945b5bab5b8f0d110c503'
                                    },
                                    'syslog_drain_url': null,
                                    'label': 'SqlServer',
                                    'provider': null,
                                    'plan': 'sharedVM',
                                    'name': 'mySqlServerService',
                                    'tags': [
                                        'sqlserver'
                                    ]
                                },
                            ]
                        }";

        public static string SingleServerAzureVCAP = @"
                        {
'azure-sqldb': [
        {
          'label': 'azure-sqldb',
          'provider': null,
          'plan': 'basic',
          'name': 'my-azure-db',
          'tags': [],
          'instance_name': 'my-azure-db',
          'binding_name': null,
          'credentials': {
            'sqldbName': 'u9e44b3e8e31',
            'sqlServerName': 'ud6893c77875',
            'sqlServerFullyQualifiedDomainName': 'ud6893c77875.database.windows.net',
            'databaseLogin': 'ud61c2c9ed2a',
            'databaseLoginPassword': 'yNOaMnbsjGT3qk5eW6BXcbHE6b2Da8sLcao7SdIFFqA2q345jQ9RSw==',
            'jdbcUrl': 'jdbc:sqlserver://ud6893c77875.database.windows.net:1433;database=u9e44b3e8e31;user=ud61c2c9ed2a;password=yNOaMnbsjGT3qk5eW6BXcbHE6b2Da8sLcao7SdIFFqA2q345jQ9RSw==;Encrypt=true;TrustServerCertificate=false;HostNameInCertificate=*.database.windows.net;loginTimeout=30;',
            'jdbcUrlForAuditingEnabled': 'jdbc:sqlserver://ud6893c77875.database.secure.windows.net:1433;database=u9e44b3e8e31;user=ud61c2c9ed2a;password=yNOaMnbsjGT3qk5eW6BXcbHE6b2Da8sLcao7SdIFFqA2q345jQ9RSw==;Encrypt=true;TrustServerCertificate=false;HostNameInCertificate=*.database.secure.windows.net;loginTimeout=30;',
            'hostname': 'ud6893c77875.database.windows.net',
            'port': 1433,
            'name': 'u9e44b3e8e31',
            'username': 'ud61c2c9ed2a',
            'password': 'yNOaMnbsjGT3qk5eW6BXcbHE6b2Da8sLcao7SdIFFqA2q345jQ9RSw==',
            'uri': 'mssql://ud61c2c9ed2a:yNOaMnbsjGT3qk5eW6BXcbHE6b2Da8sLcao7SdIFFqA2q345jQ9RSw%3D%3D@ud6893c77875.database.windows.net:1433/u9e44b3e8e31?encrypt=true&TrustServerCertificate=false&HostNameInCertificate=%2A.database.windows.net'
          },
          'syslog_drain_url': null,
          'volume_mounts': []
    }
      ]
                        }";

        public static string TwoServerVCAP = @"
{
    'SqlServer': [
        {
            'credentials': {
                'uid': 'uf33b2b30783a4087948c30f6c3b0c90f',
                'uri': 'jdbc:sqlserver://192.168.0.80:1433;databaseName=db1',
                'db': 'de5aa3a747c134b3d8780f8cc80be519e',
                'pw': 'Pefbb929c1e0945b5bab5b8f0d110c503'
            },
            'syslog_drain_url': null,
            'label': 'SqlServer',
            'provider': null,
            'plan': 'sharedVM',
            'name': 'mySqlServerService',
            'tags': [
                'sqlserver'
            ]
        },
        {
            'credentials': {
                'uid': 'uf33b2b30783a4087948c30f6c3b0c90f',
                'uri': 'jdbc:sqlserver://192.168.0.80:1433;databaseName=db2',
                'db': 'de5aa3a747c134b3d8780f8cc80be519e',
                'pw': 'Pefbb929c1e0945b5bab5b8f0d110c503'
            },
            'syslog_drain_url': null,
            'label': 'SqlServer',
            'provider': null,
            'plan': 'sharedVM',
            'name': 'mySqlServerService',
            'tags': [
                'sqlserver'
            ]
        },
    ]
}";
    }
}
