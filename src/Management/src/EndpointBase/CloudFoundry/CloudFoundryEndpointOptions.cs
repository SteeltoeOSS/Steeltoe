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
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Endpoint.CloudFoundry
{
    public class CloudFoundryEndpointOptions : AbstractEndpointOptions, ICloudFoundryOptions
    {
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:cloudfoundry";
        private const string VCAP_APPLICATION_ID_KEY = "vcap:application:application_id";
        private const string VCAP_APPLICATION_CLOUDFOUNDRY_API_KEY = "vcap:application:cf_api";
        private const bool Default_ValidateCertificates = true;

        public CloudFoundryEndpointOptions()
            : base()
        {
            Id = string.Empty;
        }

        public CloudFoundryEndpointOptions(IConfiguration config)
            : base(MANAGEMENT_INFO_PREFIX, config)
        {
            Id = string.Empty;
            ApplicationId = config[VCAP_APPLICATION_ID_KEY];
            CloudFoundryApi = config[VCAP_APPLICATION_CLOUDFOUNDRY_API_KEY];
        }

        public bool ValidateCertificates { get; set; } = Default_ValidateCertificates;

        public string ApplicationId { get; set; }

        public string CloudFoundryApi { get; set; }

        [Obsolete("Use Exposure Options instead.")]
        public override bool DefaultSensitive => false;
    }
}
