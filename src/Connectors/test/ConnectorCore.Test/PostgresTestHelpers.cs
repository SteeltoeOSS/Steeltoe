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

namespace Steeltoe.Connector.PostgreSql.Test
{
    public static class PostgresTestHelpers
    {
        public static string SingleServerVCAP_EDB = @"
            {
                ""EDB-Shared-PostgreSQL"": [{
                    ""credentials"": {
                        ""uri"": ""postgres://1e9e5dae-ed26-43e7-abb4-169b4c3beaff:lmu7c96mgl99b2t1hvdgd5q94v@postgres.testcloud.com:5432/1e9e5dae-ed26-43e7-abb4-169b4c3beaff""
                    },
                    ""syslog_drain_url"": null,
                    ""label"": ""EDB-Shared-PostgreSQL"",
                    ""provider"": null,
                    ""plan"": ""Basic PostgreSQL Plan"",
                    ""name"": ""myPostgres"",
                    ""tags"": [
                        ""PostgreSQL"",
                        ""Database storage""
                    ]
                }]
            }";

        public static string TwoServerVCAP_EDB = @"
            {
                ""EDB-Shared-PostgreSQL"": [{
                    ""credentials"": {
                        ""uri"": ""postgres://1e9e5dae-ed26-43e7-abb4-169b4c3beaff:lmu7c96mgl99b2t1hvdgd5q94v@postgres.testcloud.com:5432/1e9e5dae-ed26-43e7-abb4-169b4c3beaff""
                    },
                    ""syslog_drain_url"": null,
                    ""label"": ""EDB-Shared-PostgreSQL"",
                    ""provider"": null,
                    ""plan"": ""Basic PostgreSQL Plan"",
                    ""name"": ""myPostgres"",
                    ""tags"": [
                        ""PostgreSQL"",
                        ""Database storage""
                    ]
                },
                {
                    ""credentials"": {
                        ""uri"": ""postgres://1e9e5dae-ed26-43e7-abb4-169b4c3beaff:lmu7c96mgl99b2t1hvdgd5q94v@postgres.testcloud.com:5432/1e9e5dae-ed26-43e7-abb4-169b4c3beaff""
                    },
                    ""syslog_drain_url"": null,
                    ""label"": ""EDB-Shared-PostgreSQL"",
                    ""provider"": null,
                    ""plan"": ""Basic PostgreSQL Plan"",
                    ""name"": ""myPostgres1"",
                    ""tags"": [
                        ""PostgreSQL"",
                        ""Database storage""
                    ]
                }]
            }";

        public static string SingleServerVCAP_Crunchy = @"
            {
                ""postgresql-10-odb"": [{
                    ""name"": ""myPostgres"",
                    ""instance_name"": ""myPostgres"",
                    ""binding_name"": null,
                    ""credentials"": {
                        ""db_host"": ""10.194.45.174"",
                        ""db_name"": ""postgresample"",
                        ""db_port"": 5432,
                        ""jdbc_read_uri"": ""jdbc:postgresql://10.194.45.174:5432/postgresample"",
                        ""jdbc_uri"": ""jdbc:postgresql://10.194.45.174:5432/postgresample"",
                        ""password"": ""!DQ4Wm!r4omt$h1929!$"",
                        ""read_host"": ""10.194.45.174"",
                        ""read_port"": 5432,
                        ""read_uri"": ""postgresql://steeltoe7b59f5b8a34bce2a3cf873061cfb5815:%21DQ4Wm%21r4omt%24h1929%21%24@10.194.45.174:5432/postgresample"",
                        ""service_id"": ""service-instance_9d294ea3-4745-4115-8ef2-0ee28f42bc78"",
                        ""service_role"": ""steeltoe"",
                        ""uri"": ""postgresql://steeltoe7b59f5b8a34bce2a3cf873061cfb5815:%21DQ4Wm%21r4omt%24h1929%21%24@10.194.45.174:5432/postgresample?sslmode=require&pooling=true"",
                        ""username"": ""steeltoe7b59f5b8a34bce2a3cf873061cfb5815""
                    },
                    ""syslog_drain_url"": null,
                    ""volume_mounts"": [],
                    ""label"": ""postgresql-10-odb"",
                    ""provider"": null,
                    ""plan"": ""standalone"",
                    ""tags"": [
                        ""crunchy"",
                        ""postgresql"",
                        ""postgresql-10"",
                        ""on-demand""
                    ]
                }]
            }";

        public static string SingleServerVCAP_Azure = @"
            {
                ""azure-postgresql-9-6"": [{
                    ""name"": ""azure-beetmssql"",
                    ""instance_name"": ""azure-beetmssql"",
                    ""binding_name"": null,
                    ""credentials"": {
                        ""host"": ""2980cfbe-e198-46fd-8f81-966584bb4678.postgres.database.azure.com"",
                        ""port"": 5432,
                        ""database"": ""g01w0qnrb7"",
                        ""username"": ""c2cdhwt4nd@2980cfbe-e198-46fd-8f81-966584bb4678"",
                        ""password"": ""Dko4PGJAsQyEj5gj"",
                        ""uri"": ""postgresql://c2cdhwt4nd%402980cfbe-e198-46fd-8f81-966584bb4678:Dko4PGJAsQyEj5gj@2980cfbe-e198-46fd-8f81-966584bb4678.postgres.database.azure.com:5432/g01w0qnrb7?&sslmode=require"",
                        ""sslRequired"": true,
                        ""tags"": [
                            ""postgresql""
                        ]
                    },
                    ""syslog_drain_url"": null,
                    ""volume_mounts"": [],
                    ""label"": ""azure-postgresql-9-6"",
                    ""provider"": null,
                    ""plan"": ""basic"",
                    ""tags"": [
                        ""Azure"",
                        ""PostgreSQL"",
                        ""DBMS"",
                        ""Server"",
                        ""Database""
                    ]
                }]
            }";
    }
}
