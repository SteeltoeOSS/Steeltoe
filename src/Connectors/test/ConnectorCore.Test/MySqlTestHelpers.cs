// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connector.MySql.Test;

public static class MySqlTestHelpers
{
    public static string SingleServerVCAP = @"
            {
                ""p-mysql"": [
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
                    ""name"": ""spring-cloud-broker-db"",
                    ""tags"": [
                        ""mysql"",
                        ""relational""
                    ]
                }]
            }";

    public static string SingleServerAzureVCAP = @"
            {
                ""azure-mysql-5-7"": [{
                    ""name"": ""azure-beetmysql"",
                    ""instance_name"": ""azure-beetmysql"",
                    ""binding_name"": null,
                    ""credentials"": {
                        ""host"": ""451200b4-c29d-4346-9a0a-70bc109bb6e9.mysql.database.azure.com"",
                        ""port"": 3306,
                        ""database"": ""ub6oyk1kkh"",
                        ""username"": ""wj7tsxai7i@451200b4-c29d-4346-9a0a-70bc109bb6e9"",
                        ""password"": ""10PUO82Uhqk8F2ii"",
                        ""sslRequired"": true,
                        ""uri"": ""mysql://wj7tsxai7i%40451200b4-c29d-4346-9a0a-70bc109bb6e9:10PUO82Uhqk8F2ii@451200b4-c29d-4346-9a0a-70bc109bb6e9.mysql.database.azure.com:3306/ub6oyk1kkh?useSSL=true&requireSSL=true"",
                        ""tags"": [
                            ""mysql""
                        ]
                    },
                    ""syslog_drain_url"": null,
                    ""volume_mounts"": [],
                    ""label"": ""azure-mysql-5-7"",
                    ""provider"": null,
                    ""plan"": ""basic"",
                    ""tags"": [
                        ""Azure"",
                        ""MySQL"",
                        ""DBMS"",
                        ""Server"",
                        ""Database""
                    ]
                }]
            }";

    public static string TwoServerVCAP = @"
            {
                ""p-mysql"": [
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