// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Connector.Redis.Test;

public static class RedisCacheTestHelpers
{
    public static string SingleServerVcap = @"
            {
                ""p-redis"": [{
                    ""credentials"": {
                        ""host"": ""192.168.0.103"",
                        ""password"": ""133de7c8-9f3a-4df1-8a10-676ba7ddaa10"",
                        ""port"": 60287
                    },
                    ""syslog_drain_url"": null,
                    ""label"": ""p-redis"",
                    ""provider"": null,
                    ""plan"": ""shared-vm"",
                    ""name"": ""myRedisService1"",
                    ""tags"": [
                        ""pivotal"",
                        ""redis""
                    ]
                }]
            }";

    public static string TwoServerVcap = @"
            {
                ""p-redis"": [{
                    ""credentials"": {
                        ""host"": ""192.168.0.103"",
                        ""password"": ""133de7c8-9f3a-4df1-8a10-676ba7ddaa10"",
                        ""port"": 60287
                    },
                    ""syslog_drain_url"": null,
                    ""label"": ""p-redis"",
                    ""provider"": null,
                    ""plan"": ""shared-vm"",
                    ""name"": ""myRedisService1"",
                    ""tags"": [
                        ""pivotal"",
                        ""redis""
                    ]
                }, 
                {
                    ""credentials"": {
                        ""host"": ""192.168.0.103"",
                        ""password"": ""133de7c8-9f3a-4df1-8a10-676ba7ddaa10"",
                        ""port"": 60287
                    },
                    ""syslog_drain_url"": null,
                    ""label"": ""p-redis"",
                    ""provider"": null,
                    ""plan"": ""shared-vm"",
                    ""name"": ""myRedisService2"",
                    ""tags"": [
                        ""pivotal"",
                        ""redis""
                    ]
                }]
            }";

    public static Dictionary<string, string> SingleServerAsDictionary = new ()
    {
        { "services:p-redis:0:name", "myRedisService" },
        { "services:p-redis:0:tags:0", "redis" },
        { "services:p-redis:0:credentials:host", "192.168.0.103" },
        { "services:p-redis:0:credentials:password", "133de7c8-9f3a-4df1-8a10-676ba7ddaa10" },
        { "services:p-redis:0:credentials:port", "60287" },
    };
}
