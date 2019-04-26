// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Steeltoe.CloudFoundry.Connector.Redis.Test
{
    public static class RedisCacheTestHelpers
    {
        public static string SingleServerVCAP = @"
{
      'p-redis': [
        {
            'credentials': {
                'host': '192.168.0.103',
                'password': '133de7c8-9f3a-4df1-8a10-676ba7ddaa10',
                'port': 60287
            },
          'syslog_drain_url': null,
          'label': 'p-redis',
          'provider': null,
          'plan': 'shared-vm',
          'name': 'myRedisService1',
          'tags': [
            'pivotal',
            'redis'
          ]
        }
      ]
}";

        public static string SingleServerVCAP_AzureBroker = @"
{
      'azure-rediscache': [
        {
          'name': 'azure-redis',
          'instance_name': 'azure-redis',
          'binding_name': null,
          'credentials': {
            'host': 'cbe9d9a0-6502-438d-87ec-f26f1974e378.redis.cache.windows.net',
            'port': 6379,
            'password': 'V+4dv03jSUZkEcjGhVMR0hjEPfILCCcth1JE8vPRki4=',
            'uri': 'redis://:V%2B4dv03jSUZkEcjGhVMR0hjEPfILCCcth1JE8vPRki4%3D@cbe9d9a0-6502-438d-87ec-f26f1974e378.redis.cache.windows.net:6379'
          },
          'syslog_drain_url': null,
          'volume_mounts': [],
          'label': 'azure-rediscache',
          'provider': null,
          'plan': 'basic',
          'tags': [
            'Azure',
            'Redis',
            'Cache',
            'Database'
          ]
    }
      ]
    }";
        public static string TwoServerVCAP = @"
{
      'p-redis': [
        {
            'credentials': {
                'host': '192.168.0.103',
                'password': '133de7c8-9f3a-4df1-8a10-676ba7ddaa10',
                'port': 60287
            },
          'syslog_drain_url': null,
          'label': 'p-redis',
          'provider': null,
          'plan': 'shared-vm',
          'name': 'myRedisService1',
          'tags': [
            'pivotal',
            'redis'
          ]
        }, 
        {
            'credentials': {
                'host': '192.168.0.103',
                'password': '133de7c8-9f3a-4df1-8a10-676ba7ddaa10',
                'port': 60287
            },
          'syslog_drain_url': null,
          'label': 'p-redis',
          'provider': null,
          'plan': 'shared-vm',
          'name': 'myRedisService2',
          'tags': [
            'pivotal',
            'redis'
          ]
        } 
      ]
}";
    }
}
