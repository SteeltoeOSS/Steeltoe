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

namespace Steeltoe.Connector.Test
{
    public class GemFireTestHelpers
    {
        public static string SingleBinding_PivotalCloudCache_DevPlan_VCAP = @"
            {
                ""p-cloudcache"": [
                    {
                      ""name"": ""pcc-dev"",
                      ""instance_name"": ""pcc-dev"",
                      ""binding_name"": null,
                      ""credentials"": {
                        ""distributed_system_id"": ""0"",
                        ""locators"": [
                          ""10.194.45.168[55221]""
                        ],
                        ""urls"": {
                          ""gfsh"": ""https://cloudcache-4126e12a-508a-40f3-8df6-2cf7157f3095.cf.beet.springapps.io/gemfire/v1"",
                          ""pulse"": ""https://cloudcache-4126e12a-508a-40f3-8df6-2cf7157f3095.cf.beet.springapps.io/pulse""
                        },
                        ""users"": [
                          {
                            ""password"": ""nOVqiwOmLL6O54lGmlcfw"",
                            ""roles"": [
                              ""cluster_operator""
                            ],
                            ""username"": ""cluster_operator_ftqmv76NgWDu8q8vNvxuQQ""
                          },
                          {
                            ""password"": ""MGMtLoPDToFXlfnFhYZpA"",
                            ""roles"": [
                              ""developer""
                            ],
                            ""username"": ""developer_zcs4XnFoWIDg14VVA7GKxA""
                          }
                        ],
                        ""wan"": {
                          ""sender_credentials"": {
                            ""active"": {
                              ""password"": ""INFbDQVjvxunZ1nl9ObSQ"",
                              ""username"": ""gateway_sender_gaogaWQwOlJbSg4qItqEg""
                            }
                          }
                        }
                      },
                      ""syslog_drain_url"": null,
                      ""volume_mounts"": [],
                      ""label"": ""p-cloudcache"",
                      ""provider"": null,
                      ""plan"": ""dev-plan"",
                      ""tags"": [
                        ""gemfire"",
                        ""cloudcache"",
                        ""database"",
                        ""pivotal""
                      ]
                    }
                ]
            }";

        public static string MultipleBindings_PivotalCloudCache_VCAP = @"
            {
                ""p-cloudcache"": [
                    {
                      ""name"": ""pcc-dev"",
                      ""instance_name"": ""pcc-dev"",
                      ""binding_name"": null,
                      ""credentials"": {
                        ""distributed_system_id"": ""0"",
                        ""locators"": [
                          ""10.194.45.168[55221]""
                        ],
                        ""urls"": {
                          ""gfsh"": ""https://cloudcache-4126e12a-508a-40f3-8df6-2cf7157f3095.cf.beet.springapps.io/gemfire/v1"",
                          ""pulse"": ""https://cloudcache-4126e12a-508a-40f3-8df6-2cf7157f3095.cf.beet.springapps.io/pulse""
                        },
                        ""users"": [
                          {
                            ""password"": ""nOVqiwOmLL6O54lGmlcfw"",
                            ""roles"": [
                              ""cluster_operator""
                            ],
                            ""username"": ""cluster_operator_ftqmv76NgWDu8q8vNvxuQQ""
                          },
                          {
                            ""password"": ""MGMtLoPDToFXlfnFhYZpA"",
                            ""roles"": [
                              ""developer""
                            ],
                            ""username"": ""developer_zcs4XnFoWIDg14VVA7GKxA""
                          }
                        ],
                        ""wan"": {
                          ""sender_credentials"": {
                            ""active"": {
                              ""password"": ""INFbDQVjvxunZ1nl9ObSQ"",
                              ""username"": ""gateway_sender_gaogaWQwOlJbSg4qItqEg""
                            }
                          }
                        }
                      },
                      ""syslog_drain_url"": null,
                      ""volume_mounts"": [],
                      ""label"": ""p-cloudcache"",
                      ""provider"": null,
                      ""plan"": ""dev-plan"",
                      ""tags"": [
                        ""gemfire"",
                        ""cloudcache"",
                        ""database"",
                        ""pivotal""
                      ]
                    },
                    {
                      ""name"": ""pcc-smf"",
                      ""instance_name"": ""pcc-smf"",
                      ""binding_name"": null,
                      ""credentials"": {
                        ""distributed_system_id"": ""0"",
                        ""locators"": [
                          ""10.194.45.191[55221]"",
                          ""10.194.45.188[55221]"",
                          ""10.194.45.190[55221]""
                        ],
                        ""urls"": {
                          ""gfsh"": ""https://cloudcache-78d30060-cf52-43dc-9b71-d2df6e314829.cf.beet.springapps.io/gemfire/v1"",
                          ""pulse"": ""https://cloudcache-78d30060-cf52-43dc-9b71-d2df6e314829.cf.beet.springapps.io/pulse""
                        },
                        ""users"": [
                          {
                            ""password"": ""dNOrjKbqvVpkYCVld0BOUg"",
                            ""roles"": [
                              ""cluster_operator""
                            ],
                            ""username"": ""cluster_operator_Qfwtl8PXiI723H6PO5PpxQ""
                          },
                          {
                            ""password"": ""a04RMKLFPjYmYS4M5GvY0A"",
                            ""roles"": [
                              ""developer""
                            ],
                            ""username"": ""developer_G5XmmQBfhIXyz0vgcAOEg""
                          }
                        ],
                        ""wan"": {
                          ""sender_credentials"": {
                            ""active"": {
                              ""password"": ""K7Wrtf931zebyz2FBl30w"",
                              ""username"": ""gateway_sender_mKDd7drblH7Oxi50tyovQ""
                            }
                          }
                        }
                      },
                      ""syslog_drain_url"": null,
                      ""volume_mounts"": [],
                      ""label"": ""p-cloudcache"",
                      ""provider"": null,
                      ""plan"": ""small-footprint"",
                      ""tags"": [
                        ""gemfire"",
                        ""cloudcache"",
                        ""database"",
                        ""pivotal""
                      ]
                    }
                ]
            }";
    }
}
