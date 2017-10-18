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

namespace Steeltoe.CloudFoundry.Connector.PostgreSql.Test
{
    public static class PostgresTestHelpers
    {
        public static string SingleServerVCAP = @"
{
        'EDB-Shared-PostgreSQL': [
            {
                'credentials': {
                    'uri': 'postgres://1e9e5dae-ed26-43e7-abb4-169b4c3beaff:lmu7c96mgl99b2t1hvdgd5q94v@postgres.testcloud.com:5432/1e9e5dae-ed26-43e7-abb4-169b4c3beaff'
                },
            'syslog_drain_url': null,
            'label': 'EDB-Shared-PostgreSQL',
            'provider': null,
            'plan': 'Basic PostgreSQL Plan',
            'name': 'myPostgres',
            'tags': [
                'PostgreSQL',
                'Database storage'
            ]
        }
      ]
}";
        public static string TwoServerVCAP = @"
{
        'EDB-Shared-PostgreSQL': [
            {
                'credentials': {
                    'uri': 'postgres://1e9e5dae-ed26-43e7-abb4-169b4c3beaff:lmu7c96mgl99b2t1hvdgd5q94v@postgres.testcloud.com:5432/1e9e5dae-ed26-43e7-abb4-169b4c3beaff'
                },
            'syslog_drain_url': null,
            'label': 'EDB-Shared-PostgreSQL',
            'provider': null,
            'plan': 'Basic PostgreSQL Plan',
            'name': 'myPostgres',
            'tags': [
                'PostgreSQL',
                'Database storage'
            ]
        },
        {
            'credentials': {
                'uri': 'postgres://1e9e5dae-ed26-43e7-abb4-169b4c3beaff:lmu7c96mgl99b2t1hvdgd5q94v@postgres.testcloud.com:5432/1e9e5dae-ed26-43e7-abb4-169b4c3beaff'
            },
            'syslog_drain_url': null,
            'label': 'EDB-Shared-PostgreSQL',
            'provider': null,
            'plan': 'Basic PostgreSQL Plan',
            'name': 'myPostgres1',
            'tags': [
                'PostgreSQL',
                'Database storage'
            ]
        }
      ]
}";
    }
}
