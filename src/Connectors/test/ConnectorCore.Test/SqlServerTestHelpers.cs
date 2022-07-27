// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connector.SqlServer.Test;

public class SqlServerTestHelpers
{
    public static string SingleServerVCAP = @"
            {
                ""SqlServer"": [
                    {
                        ""credentials"": {
                            ""uid"": ""uf33b2b30783a4087948c30f6c3b0c90f"",
                            ""uri"": ""jdbc:sqlserver://192.168.0.80:1433;databaseName=de5aa3a747c134b3d8780f8cc80be519e"",
                            ""db"": ""de5aa3a747c134b3d8780f8cc80be519e"",
                            ""pw"": ""Pefbb929c1e0945b5bab5b8f0d110c503""
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""SqlServer"",
                        ""provider"": null,
                        ""plan"": ""sharedVM"",
                        ""name"": ""mySqlServerService"",
                        ""tags"": [
                            ""sqlserver""
                        ]
                    },
                ]
            }";

    public static string SingleServerAzureVCAP = @"
            {
                ""azure-sql-12-0"": [{
                    ""name"": ""azure-beetmssql"",
                    ""instance_name"": ""azure-beetmssql"",
                    ""binding_name"": null,
                    ""credentials"": {
                        ""host"": ""fe049939-64f1-44f5-9f84-073ed5c82088.database.windows.net"",
                        ""port"": 1433,
                        ""database"": ""f1egl8ify4"",
                        ""username"": ""rgmm5zlri4"",
                        ""password"": ""737mAU1pj6HcBxzw"",
                        ""uri"": ""sqlserver://rgmm5zlri4:737mAU1pj6HcBxzw@fe049939-64f1-44f5-9f84-073ed5c82088.database.windows.net:1433/f1egl8ify4;encrypt=true;trustServerCertificate=true"",
                        ""tags"": null,
                        ""jdbcUrl"": ""jdbc:sqlserver://fe049939-64f1-44f5-9f84-073ed5c82088.database.windows.net:1433;database=f1egl8ify4;user=rgmm5zlri4;password=737mAU1pj6HcBxzw;encrypt=true;trustServerCertificate=true;"",
                        ""encrypt"": true
                    },
                    ""syslog_drain_url"": null,
                    ""volume_mounts"": [],
                    ""label"": ""azure-sql-12-0"",
                    ""provider"": null,
                    ""plan"": ""basic"",
                    ""tags"": [
                        ""Azure"",
                        ""SQL"",
                        ""DBMS"",
                        ""Server"",
                        ""Database""
                    ]
                }]
            }";

    public static string SingleServerVCAPNoTag = @"
            {
                ""SqlServer"": [
                    {
                        ""credentials"": {
                            ""uid"": ""uf33b2b30783a4087948c30f6c3b0c90f"",
                            ""uri"": ""sqlserver://192.168.0.80:1433;databaseName=de5aa3a747c134b3d8780f8cc80be519e"",
                            ""db"": ""de5aa3a747c134b3d8780f8cc80be519e"",
                            ""pw"": ""Pefbb929c1e0945b5bab5b8f0d110c503""
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""SqlServer"",
                        ""provider"": null,
                        ""plan"": ""sharedVM"",
                        ""name"": ""mySqlServerService"",
                        ""tags"": [
                        ]
                    },
                ]
            }";

    public static string SingleServerVCAPIgnoreName = @"
            {
                ""user-provided"": [{
                    ""name"": ""sql-server-config-user-provided-service"",
                    ""instance_name"": ""sql-server-config-user-provided-service"",
                    ""binding_name"": null,
                    ""credentials"": {
                        ""db"": ""testdb"",
                        ""URI"": ""sqlserver://ajaganathansqlserver:1433/testdb""
                    },
                    ""syslog_drain_url"": """",
                    ""volume_mounts"": [],
                    ""label"": ""user-provided"",
                    ""tags"": []
                }]
            }";

    public static string TwoServerVCAP = @"
            {
                ""SqlServer"": [{
                        ""credentials"": {
                            ""uid"": ""uf33b2b30783a4087948c30f6c3b0c90f"",
                            ""uri"": ""jdbc:sqlserver://192.168.0.80:1433;databaseName=db1"",
                            ""db"": ""de5aa3a747c134b3d8780f8cc80be519e"",
                            ""pw"": ""Pefbb929c1e0945b5bab5b8f0d110c503""
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""SqlServer"",
                        ""provider"": null,
                        ""plan"": ""sharedVM"",
                        ""name"": ""mySqlServerService"",
                        ""tags"": [
                            ""sqlserver""
                        ]
                    },
                    {
                        ""credentials"": {
                            ""uid"": ""uf33b2b30783a4087948c30f6c3b0c90f"",
                            ""uri"": ""jdbc:sqlserver://192.168.0.80:1433;databaseName=db2"",
                            ""db"": ""de5aa3a747c134b3d8780f8cc80be519e"",
                            ""pw"": ""Pefbb929c1e0945b5bab5b8f0d110c503""
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""SqlServer"",
                        ""provider"": null,
                        ""plan"": ""sharedVM"",
                        ""name"": ""mySqlServerService"",
                        ""tags"": [
                            ""sqlserver""
                        ]
                    },
                ]
            }";
}