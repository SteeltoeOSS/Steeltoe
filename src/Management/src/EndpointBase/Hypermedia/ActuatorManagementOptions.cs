﻿// Copyright 2017 the original author or authors.
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
using Steeltoe.Common;
using System;

namespace Steeltoe.Management.Endpoint.Hypermedia
{
    public class ActuatorManagementOptions : ManagementEndpointOptions
    {
        private const string DEFAULT_ACTUATOR_PATH = "/actuator";

        public Exposure Exposure { get; set; }

        public ActuatorManagementOptions()
        {
            Path = DEFAULT_ACTUATOR_PATH;
            Exposure = new Exposure();
        }

        public ActuatorManagementOptions(IConfiguration config)
            : base(config)
        {
            if (string.IsNullOrEmpty(Path))
            {
                Path = DEFAULT_ACTUATOR_PATH;
            }

            if (Platform.IsCloudFoundry && Path.StartsWith("/cloudfoundryapplication", StringComparison.OrdinalIgnoreCase))
            {
                Path = DEFAULT_ACTUATOR_PATH; // Override path set to /cloudfoundryapplication since it will be hidden by the cloudfoundry context actuators
            }

            Exposure = new Exposure(config);
        }
    }
}
