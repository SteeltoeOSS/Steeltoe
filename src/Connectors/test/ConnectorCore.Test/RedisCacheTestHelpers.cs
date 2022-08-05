// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connector.Redis.Test;

public static class RedisCacheTestHelpers
{
    public const string SingleServerVcap = @"
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

    public const string SingleServerEnterpriseVcap = @"
            {
                ""redislabs"": [{
                    ""label"": ""redislabs"",
                    ""provider"": null,
                    ""plan"": ""medium-redis"",
                    ""name"": ""myRedisService"",
                    ""tags"": [
                        ""redislabs"",
                        ""redis""
                    ],
                    ""instance_name"": ""myRedisService"",
                    ""binding_name"": null,
                    ""credentials"": {
                        ""host"": ""redis-1076.redis-enterprise.system.cloudyazure.io"",
                        ""ip_list"": [
                            ""10.0.12.37""
                        ],
                        ""name"": ""cf-7080d854-50a5-413d-82cb-4cb0fc93e4b9"",
                        ""password"": ""rQrMqqg-.LJzO498EcAIfp-auu4czBiGM40wjveTdHw-EJu0"",
                        ""port"": 1076
                    },
                    ""syslog_drain_url"": null,
                    ""volume_mounts"": []
                }]
            }";

    public const string SingleServerVcapAzureBroker = @"
            {
                ""azure-rediscache"": [{
                    ""name"": ""azure-redis"",
                    ""instance_name"": ""azure-redis"",
                    ""binding_name"": null,
                    ""credentials"": {
                        ""host"": ""cbe9d9a0-6502-438d-87ec-f26f1974e378.redis.cache.windows.net"",
                        ""port"": 6379,
                        ""password"": ""V+4dv03jSUZkEcjGhVMR0hjEPfILCCcth1JE8vPRki4="",
                        ""uri"": ""redis://:V%2B4dv03jSUZkEcjGhVMR0hjEPfILCCcth1JE8vPRki4%3D@cbe9d9a0-6502-438d-87ec-f26f1974e378.redis.cache.windows.net:6379""
                    },
                    ""syslog_drain_url"": null,
                    ""volume_mounts"": [],
                    ""label"": ""azure-rediscache"",
                    ""provider"": null,
                    ""plan"": ""basic"",
                    ""tags"": [
                        ""Azure"",
                        ""Redis"",
                        ""Cache"",
                        ""Database""
                    ]
                }]
            }";

    public const string SingleServerVcapAzureBrokerSecure = @"
            {
                ""azure-rediscache"": [{
                    ""name"": ""redis"",
                    ""instance_name"": ""redis"",
                    ""binding_name"": null,
                    ""credentials"": {
                        ""host"": ""9b67c347-03b8-4956-aa2a-858ac30aced5.redis.cache.windows.net"",
                        ""port"": 6380,
                        ""password"": ""mAG0+CdozukoUTOIEAo6wTKHdMoqg4+Jfno8docw3Zg="",
                        ""uri"": ""rediss://:mAG0%2BCdozukoUTOIEAo6wTKHdMoqg4%2BJfno8docw3Zg%3D@9b67c347-03b8-4956-aa2a-858ac30aced5.redis.cache.windows.net:6380""
                    },
                    ""syslog_drain_url"": null,
                    ""volume_mounts"": [],
                    ""label"": ""azure-rediscache"",
                    ""provider"": null,
                    ""plan"": ""basic"",
                    ""tags"": [
                        ""Azure"",
                        ""Redis"",
                        ""Cache"",
                        ""Database""
                    ]
                }]
            }";

    public const string TwoServerVcap = @"
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
}
