// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connector.MongoDb.Test
{
    public static class MongoDbTestHelpers
    {
        /// <summary>
        /// Sample VCAP_SERVICES entry for a9s MongoDB for PCF
        /// </summary>
        public static string SingleBinding_a9s_SingleServer_VCAP = @"
            {
                ""a9s-mongodb36"": [{
                    ""name"": ""steeltoe"",
                    ""instance_name"": ""steeltoe"",
                    ""binding_name"": null,
                    ""credentials"": {
                        ""username"": ""a9s-brk-usr-377ad48194cbf0452338737d7f6aa3fb6cdabc24"",
                        ""password"": ""a9se01c566aab8dab674e7243b688c3011df74bda30"",
                        ""hosts"": [""d8790b7-mongodb-0.node.dc1.a9s-mongodb-consul:27017""],
                        ""default_database"": ""d8790b7"",
                        ""uri"": ""mongodb://a9s-brk-usr-377ad48194cbf0452338737d7f6aa3fb6cdabc24:a9se01c566aab8dab674e7243b688c3011df74bda30@d8790b7-mongodb-0.node.dc1.a9s-mongodb-consul:27017/d8790b7"",
                        ""dns_servers"": [
                            ""10.194.45.174"",
                            ""10.194.45.176"",
                            ""10.194.45.177""
                        ],
                        ""host_ips"": [""10.194.45.168:27017""]
                    },
                    ""syslog_drain_url"": null,
                    ""volume_mounts"": [],
                    ""label"": ""a9s-mongodb36"",
                    ""provider"": null,
                    ""plan"": ""mongodb-single-nano"",
                    ""tags"": [
                        ""nosql"",
                        ""database"",
                        ""document store"",
                        ""eventual consistent""
                    ]
                }]
            }";

        /// <summary>
        /// Sample VCAP_SERVICES entry for a9s MongoDB with replicas
        /// </summary>
        public static string SingleBinding_a9s_WithReplicas_VCAP = @"
            {
                ""a9s-mongodb36"": [{
                    ""name"": ""steeltoe"",
                    ""instance_name"": ""steeltoe"",
                    ""binding_name"": null,
                    ""credentials"": {
                        ""username"": ""a9s-brk-usr-e74b9538ae5dcf04500eb0fc18907338d4610f30"",
                        ""password"": ""a9sb8b69cc8a724d867f298cd0d1f0bd07ccdf90a86"",
                        ""hosts"": [
                            ""d5584e9-mongodb-0.node.dc1.a9s-mongodb-consul:27017"",
                            ""d5584e9-mongodb-1.node.dc1.a9s-mongodb-consul:27017"",
                            ""d5584e9-mongodb-2.node.dc1.a9s-mongodb-consul:27017""
                        ],
                        ""default_database"": ""d5584e9"",
                        ""uri"": ""mongodb://a9s-brk-usr-e74b9538ae5dcf04500eb0fc18907338d4610f30:a9sb8b69cc8a724d867f298cd0d1f0bd07ccdf90a86@d5584e9-mongodb-0.node.dc1.a9s-mongodb-consul:27017,d5584e9-mongodb-1.node.dc1.a9s-mongodb-consul:27017,d5584e9-mongodb-2.node.dc1.a9s-mongodb-consul:27017/d5584e9?replicaSet=rs0"",
                        ""dns_servers"": [
                            ""10.194.45.174"",
                            ""10.194.45.176"",
                            ""10.194.45.177""
                        ],
                        ""host_ips"": [
                            ""10.194.45.189:27017"",
                            ""10.194.45.190:27017"",
                            ""10.194.45.191:27017""
                        ]
                    },
                    ""syslog_drain_url"": null,
                    ""volume_mounts"": [],
                    ""label"": ""a9s-mongodb36"",
                    ""provider"": null,
                    ""plan"": ""mongodb-replica-small"",
                    ""tags"": [
                        ""nosql"",
                        ""database"",
                        ""document store"",
                        ""eventual consistent""
                    ]
                }]
            }";

        /// <summary>
        /// Sample VCAP_SERVICES entry for MongoDB Enterprise Service for PCF
        /// </summary>
        public static string SingleServer_Enterprise_VCAP = @"
            {
                ""mongodb-odb"": [{
                    ""name"": ""steeltoe"",
                    ""instance_name"": ""steeltoe"",
                    ""binding_name"": null,
                    ""credentials"": {
                        ""database"": ""default"",
                        ""password"": ""9fb82def23aa8d5f90a0dd21323d06e0"",
                        ""servers"": [
                            ""192.168.12.22:28000""
                        ],
                        ""uri"": ""mongodb://pcf_b8ce63777ce39d1c7f871f2585ba9474:9fb82def23aa8d5f90a0dd21323d06e0@192.168.12.22:28000/default?authSource=admin"",
                        ""username"": ""pcf_b8ce63777ce39d1c7f871f2585ba9474""
                    },
                    ""syslog_drain_url"": null,
                    ""volume_mounts"": [],
                    ""label"": ""mongodb-odb"",
                    ""provider"": null,
                    ""plan"": ""standalone_small"",
                    ""tags"": [
                        ""mongodb""
                    ]
                }]
            }";

        /// <summary>
        /// Sample VCAP_SERVICES entry for Azure's CosmoDB via MongoDB API
        /// </summary>
        public static string SingleServer_CosmosDb_VCAP = @"
            {
                ""azure-cosmosdb"": [{
                    ""label"": ""azure-cosmosdb"",
                    ""provider"": null,
                    ""plan"": ""standard"",
                    ""name"": ""mongoCosmos"",
                    ""tags"": [],
                    ""instance_name"": ""mongoCosmos"",
                    ""binding_name"": """",
                    ""credentials"": {
                        ""cosmosdb_host_endpoint"": ""https://u83bde2c09fd.documents.azure.com:10255/"",
                        ""cosmosdb_username"": ""u83bde2c09fd"",
                        ""cosmosdb_password"": ""36SWUyZbIyuu4AwLWMbAal9QngyVbZJjyoH9m0kILXIiEA9fCUhb34JHOovSNk2jpbXEQDuGnsBrYj1vTlu41w=="",
                        ""cosmosdb_readonly_password"": ""36SWUyZbIyuu4AwLWMbAal9QngyVbZJjyoH9m0kILXIiEA9fCUhb34JHOovSNk2jpbXEQDuGnsBrYj1vTlu41w=="",
                        ""cosmosdb_connection_string"": ""mongodb://u83bde2c09fd:36SWUyZbIyuu4AwLWMbAal9QngyVbZJjyoH9m0kILXIiEA9fCUhb34JHOovSNk2jpbXEQDuGnsBrYj1vTlu41w==@u83bde2c09fd.documents.azure.com:10255/?ssl=true&replicaSet=globaldb"",
                        ""cosmosdb_readonly_connection_string"": ""mongodb://u83bde2c09fd:36SWUyZbIyuu4AwLWMbAal9QngyVbZJjyoH9m0kILXIiEA9fCUhb34JHOovSNk2jpbXEQDuGnsBrYj1vTlu41w==@u83bde2c09fd.documents.azure.com:10255/?ssl=true&replicaSet=globaldb""
                    },
                    ""syslog_drain_url"": null,
                    ""volume_mounts"": []
                }]
            }";
    }
}
