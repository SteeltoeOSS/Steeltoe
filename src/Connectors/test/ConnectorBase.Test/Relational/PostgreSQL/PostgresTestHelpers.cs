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

namespace Steeltoe.CloudFoundry.Connector.PostgreSql.Test
{
    public static class PostgresTestHelpers
    {
        public static string SingleServerVCAP_EDB = @"
            {
                ""EDB-Shared-PostgreSQL"": [
                {
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
                ""postgresql-9.5-odb"": [{
                    ""credentials"": {
                        ""db_host"": ""10.194.59.205"",
                        ""db_name"": ""steeltoe"",
                        ""db_port"": 5432,
                        ""jdbc_read_uri"": ""jdbc:postgresql://10.194.59.205:5433/steeltoe"",
                        ""jdbc_uri"": ""jdbc:postgresql://10.194.59.205:5432/steeltoe"",
                        ""password"": ""Qp!1mB1$Zk2T!$!D85_E"",
                        ""read_host"": ""10.194.59.205"",
                        ""read_port"": 5433,
                        ""read_uri"": ""postgresql://testrolee93ccf859894dc60dcd53218492b37b4:Qp!1mB1$Zk2T!$!D85_E@10.194.59.205:5433/steeltoe"",
                        ""service_id"": ""service-instance_1eb741c0-dcf7-41ab-97c3-d5eeb5bbf559"",
                        ""uri"": ""postgresql://testrolee93ccf859894dc60dcd53218492b37b4:Qp!1mB1$Zk2T!$!D85_E@10.194.59.205:5432/steeltoe"",
                        ""username"": ""testrolee93ccf859894dc60dcd53218492b37b4""
                    },
                    ""syslog_drain_url"": null,
                    ""volume_mounts"": [],
                    ""label"": ""postgresql-9.5-odb"",
                    ""provider"": null,
                    ""plan"": ""small"",
                    ""name"": ""myPostgres"",
                    ""tags"": [
                        ""crunchy"",
                        ""postgresql"",
                        ""postgresql-9.5"",
                        ""on-demand""
                    ]
                }]
            }";
    }
}
