// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CloudFoundry.Connector.MySql.Test
{
    public static class MySqlTestHelpers
    {
        public static string SingleServerVCAP = @"
            {
                ""p-mysql"": [{
                    ""credentials"": {
                        ""hostname"": ""192.168.0.90"",
                        ""port"": 3306,
                        ""name"": ""cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355"",
                        ""username"": ""Dd6O1BPXUHdrmzbP"",
                        ""password"": ""7E1LxXnlH2hhlPVt"",
                        ""uri"": ""mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true"",
                        ""jdbcUrl"": ""jdbc:mysql://192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt""
                    },
                    ""syslog_drain_url"": null,
                    ""label"": ""p-mysql"",
                    ""provider"": null,
                    ""plan"": ""100mb-dev"",
                    ""name"": ""spring-cloud-broker-db"",
                    ""tags"": [
                        ""mysql"",
                        ""relational""
                    ]
                }]
            }";

        public static string TwoServerVCAP = @"
            {
                ""p-mysql"": [{
                    ""credentials"": {
                        ""hostname"": ""192.168.0.90"",
                        ""port"": 3306,
                        ""name"": ""cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355"",
                        ""username"": ""Dd6O1BPXUHdrmzbP"",
                        ""password"": ""7E1LxXnlH2hhlPVt"",
                        ""uri"": ""mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true"",
                        ""jdbcUrl"": ""jdbc:mysql://192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt""
                    },
                    ""syslog_drain_url"": null,
                    ""label"": ""p-mysql"",
                    ""provider"": null,
                    ""plan"": ""100mb-dev"",
                    ""name"": ""spring-cloud-broker-db"",
                    ""tags"": [
                        ""mysql"",
                        ""relational""
                    ]
                },
                {
                    ""credentials"": {
                        ""hostname"": ""192.168.0.90"",
                        ""port"": 3306,
                        ""name"": ""cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355"",
                        ""username"": ""Dd6O1BPXUHdrmzbP"",
                        ""password"": ""7E1LxXnlH2hhlPVt"",
                        ""uri"": ""mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true"",
                        ""jdbcUrl"": ""jdbc:mysql://192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt""
                    },
                    ""syslog_drain_url"": null,
                    ""label"": ""p-mysql"",
                    ""provider"": null,
                    ""plan"": ""100mb-dev"",
                    ""name"": ""spring-cloud-broker-db2"",
                    ""tags"": [
                        ""mysql"",
                        ""relational""
                    ]
                }]
            }";
    }
}
