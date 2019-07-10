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

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.EndpointOwin
{
    public static class ManagementOptions
    {
        private static IEnumerable<IManagementOptions> _mgmtOptions;

        public static IEnumerable<IManagementOptions> Get()
        {
            if (_mgmtOptions == null)
            {
                throw new Exception("Management Options not configured");
            }

            return _mgmtOptions;
        }

        public static IEnumerable<IManagementOptions> Get(IConfiguration config)
        {
            if (_mgmtOptions == null)
            {
                _mgmtOptions = new List<IManagementOptions>
                {
                     new CloudFoundryManagementOptions(config),
                     new ActuatorManagementOptions(config)
                };
            }

            return _mgmtOptions;
        }

        /// <summary>
        /// For Testing
        /// </summary>
        internal static void Clear()
        {
            _mgmtOptions = null;
        }
    }
}
