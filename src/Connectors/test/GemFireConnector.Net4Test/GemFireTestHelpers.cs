// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CloudFoundry.Connector.Test
{
    public class GemFireTestHelpers
    {
        public static string SingleBinding_PivotalCloudCache_DevPlan_VCAP = @"
            {
                'p-cloudcache': [
                    {
                      'name': 'pcc-dev',
                      'instance_name': 'pcc-dev',
                      'binding_name': null,
                      'credentials': {
                        'distributed_system_id': '0',
                        'locators': [
                          '10.194.45.168[55221]'
                        ],
                        'urls': {
                          'gfsh': 'https://cloudcache-4126e12a-508a-40f3-8df6-2cf7157f3095.cf.beet.springapps.io/gemfire/v1',
                          'pulse': 'https://cloudcache-4126e12a-508a-40f3-8df6-2cf7157f3095.cf.beet.springapps.io/pulse'
                        },
                        'users': [
                          {
                            'password': 'nOVqiwOmLL6O54lGmlcfw',
                            'roles': [
                              'cluster_operator'
                            ],
                            'username': 'cluster_operator_ftqmv76NgWDu8q8vNvxuQQ'
                          },
                          {
                            'password': 'MGMtLoPDToFXlfnFhYZpA',
                            'roles': [
                              'developer'
                            ],
                            'username': 'developer_zcs4XnFoWIDg14VVA7GKxA'
                          }
                        ],
                        'wan': {
                          'sender_credentials': {
                            'active': {
                              'password': 'INFbDQVjvxunZ1nl9ObSQ',
                              'username': 'gateway_sender_gaogaWQwOlJbSg4qItqEg'
                            }
                          }
                        }
                      },
                      'syslog_drain_url': null,
                      'volume_mounts': [],
                      'label': 'p-cloudcache',
                      'provider': null,
                      'plan': 'dev-plan',
                      'tags': [
                        'gemfire',
                        'cloudcache',
                        'database',
                        'pivotal'
                      ]
                    }
                ]
            }";

        public static string MultipleBindings_PivotalCloudCache_VCAP = @"
            {
                'p-cloudcache': [
                    {
                      'name': 'pcc-dev',
                      'instance_name': 'pcc-dev',
                      'binding_name': null,
                      'credentials': {
                        'distributed_system_id': '0',
                        'locators': [
                          '10.194.45.168[55221]'
                        ],
                        'urls': {
                          'gfsh': 'https://cloudcache-4126e12a-508a-40f3-8df6-2cf7157f3095.cf.beet.springapps.io/gemfire/v1',
                          'pulse': 'https://cloudcache-4126e12a-508a-40f3-8df6-2cf7157f3095.cf.beet.springapps.io/pulse'
                        },
                        'users': [
                          {
                            'password': 'nOVqiwOmLL6O54lGmlcfw',
                            'roles': [
                              'cluster_operator'
                            ],
                            'username': 'cluster_operator_ftqmv76NgWDu8q8vNvxuQQ'
                          },
                          {
                            'password': 'MGMtLoPDToFXlfnFhYZpA',
                            'roles': [
                              'developer'
                            ],
                            'username': 'developer_zcs4XnFoWIDg14VVA7GKxA'
                          }
                        ],
                        'wan': {
                          'sender_credentials': {
                            'active': {
                              'password': 'INFbDQVjvxunZ1nl9ObSQ',
                              'username': 'gateway_sender_gaogaWQwOlJbSg4qItqEg'
                            }
                          }
                        }
                      },
                      'syslog_drain_url': null,
                      'volume_mounts': [],
                      'label': 'p-cloudcache',
                      'provider': null,
                      'plan': 'dev-plan',
                      'tags': [
                        'gemfire',
                        'cloudcache',
                        'database',
                        'pivotal'
                      ]
                    },
                    {
                      'name': 'pcc-smf',
                      'instance_name': 'pcc-smf',
                      'binding_name': null,
                      'credentials': {
                        'distributed_system_id': '0',
                        'locators': [
                          '10.194.45.191[55221]',
                          '10.194.45.188[55221]',
                          '10.194.45.190[55221]'
                        ],
                        'urls': {
                          'gfsh': 'https://cloudcache-78d30060-cf52-43dc-9b71-d2df6e314829.cf.beet.springapps.io/gemfire/v1',
                          'pulse': 'https://cloudcache-78d30060-cf52-43dc-9b71-d2df6e314829.cf.beet.springapps.io/pulse'
                        },
                        'users': [
                          {
                            'password': 'dNOrjKbqvVpkYCVld0BOUg',
                            'roles': [
                              'cluster_operator'
                            ],
                            'username': 'cluster_operator_Qfwtl8PXiI723H6PO5PpxQ'
                          },
                          {
                            'password': 'a04RMKLFPjYmYS4M5GvY0A',
                            'roles': [
                              'developer'
                            ],
                            'username': 'developer_G5XmmQBfhIXyz0vgcAOEg'
                          }
                        ],
                        'wan': {
                          'sender_credentials': {
                            'active': {
                              'password': 'K7Wrtf931zebyz2FBl30w',
                              'username': 'gateway_sender_mKDd7drblH7Oxi50tyovQ'
                            }
                          }
                        }
                      },
                      'syslog_drain_url': null,
                      'volume_mounts': [],
                      'label': 'p-cloudcache',
                      'provider': null,
                      'plan': 'small-footprint',
                      'tags': [
                        'gemfire',
                        'cloudcache',
                        'database',
                        'pivotal'
                      ]
                    }
                ]
            }";
    }
}
