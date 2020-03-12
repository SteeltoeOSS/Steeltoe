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
                        ""hosts"": [
                            ""d8790b7-mongodb-0.node.dc1.a9s-mongodb-consul:27017""
                        ],
                        ""default_database"": ""d8790b7"",
                        ""uri"": ""mongodb://a9s-brk-usr-377ad48194cbf0452338737d7f6aa3fb6cdabc24:a9se01c566aab8dab674e7243b688c3011df74bda30@d8790b7-mongodb-0.node.dc1.a9s-mongodb-consul:27017/d8790b7"",
                        ""dns_servers"": [
                            ""10.194.45.174"",
                            ""10.194.45.176"",
                            ""10.194.45.177""
                        ],
                        ""host_ips"": [
                            ""10.194.45.168:27017""
                        ]
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
        public static string SingleBinding_Enterprise_VCAP = @"
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
        /// Sample VCAP_SERVICES entry for two instances of MongoDB Enterprise Service for PCF
        /// </summary>
        public static string DoubleBinding_Enterprise_VCAP = @"
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
                },{
                    ""name"": ""steeltoe2"",
                    ""instance_name"": ""steeltoe2"",
                    ""binding_name"": null,
                    ""credentials"": {
                        ""database"": ""default"",
                        ""password"": ""9fb82def23aa8d5f90a0dd21323d06e0-1"",
                        ""servers"": [
                            ""192.168.12.22:28001""
                        ],
                        ""uri"": ""mongodb://pcf_b8ce63777ce39d1c7f871f2585ba9474-1:9fb82def23aa8d5f90a0dd21323d06e0-1@192.168.12.22:28001/default?authSource=admin"",
                        ""username"": ""pcf_b8ce63777ce39d1c7f871f2585ba9474-1""
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
    }
}
