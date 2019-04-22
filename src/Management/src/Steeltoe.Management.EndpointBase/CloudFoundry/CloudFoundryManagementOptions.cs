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

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Endpoint.CloudFoundry
{
    public class CloudFoundryManagementOptions : ManagementEndpointOptions
    {
        private const string DEFAULT_ACTUATOR_PATH = "/cloudfoundryapplication";

        public CloudFoundryManagementOptions()
        {
            Path = DEFAULT_ACTUATOR_PATH;
        }

        public CloudFoundryManagementOptions(IConfiguration config, bool isCloudFoundry = false)
            : base(config)
        {
            if (isCloudFoundry)
            {
                Path = DEFAULT_ACTUATOR_PATH; // Ignore config path and override to /cloudfoundryapplication
            }
        }
    }
}