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

namespace Steeltoe.CloudFoundry.Connector.CosmosDb.Test
{
    public static class CosmosDbTestHelpers
    {
        public static string SingleVCAPBinding = @"
            {
                ""azure-cosmosdb"": [
                {
                    ""label"": ""azure-cosmosdb"",
                    ""provider"": null,
                    ""plan"": ""standard"",
                    ""name"": ""myCosmosDb"",
                    ""tags"": [],
                    ""instance_name"": ""myCosmosDb"",
                    ""binding_name"": """",
                    ""credentials"": {
                        ""cosmosdb_host_endpoint"": ""https://u332d11658f3.documents.azure.com:443/"",
                        ""cosmosdb_master_key"": ""lXYMGIE4mYITjXvHwQjkh0U07lwF513NdbTfeyGndeqjVXzwKQ3ZalKXQNYeIZovoyl57IY1J0KnJUH36EPufA=="",
                        ""cosmosdb_readonly_master_key"": ""hy5XZOeVnBeMmbB9FGcD54tttGKExad9XkGhn5Esc4jAM60OF2U7TcCXgffqBtBRuPAp0uFqKvz1l13OX8auPw=="",
                        ""cosmosdb_database_id"": ""u33ba24fd208"",
                        ""cosmosdb_database_link"": ""cbs/sTB+AA==/""
                    },
                    ""syslog_drain_url"": null,
                    ""volume_mounts"": []
                }]
            }";
    }
}
