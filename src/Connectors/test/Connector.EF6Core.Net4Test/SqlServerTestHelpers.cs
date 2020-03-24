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

namespace Steeltoe.Connector.SqlServer.EF6.Test
{
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
                }]
            }";
    }
}
