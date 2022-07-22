// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connector.CosmosDb.Test;

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