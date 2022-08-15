// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connector.PostgreSql.EntityFrameworkCore.Test;

public static class PostgresTestHelpers
{
    public const string SingleServerVcapEdb = @"
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

    public const string TwoServerVcapEdb = @"
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

    public const string SingleServerVcapCrunchy = @"
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

    public const string SingleServerEncodedVcapCrunchy = @"
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
                        ""read_uri"": ""postgresql://testrolee93ccf859894dc60dcd53218492b37b4:Qp%211mB1%24Zk2T%21%24%21D85_E@10.194.59.205:5433/steeltoe"",
                        ""service_id"": ""service-instance_1eb741c0-dcf7-41ab-97c3-d5eeb5bbf559"",
                        ""uri"": ""postgresql://testrolee93ccf859894dc60dcd53218492b37b4:Qp%211mB1%24Zk2T%21%24%21D85_E@10.194.59.205:5432/steeltoe"",
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
