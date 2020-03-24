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

namespace Steeltoe.Connector.MySql.EFCore.Test
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
                        ""uri"": ""mysql://Dd6O1BPXUHdrmzbP0:7E1LxXnlH2hhlPVt0@192.168.0.91:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd0407903550?reconnect=true"",
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
