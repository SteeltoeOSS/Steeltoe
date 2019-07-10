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
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Exporter.Metrics.CloudFoundryForwarder
{
    public class CloudFoundryForwarderOptions
    {
        public const string CONFIG_PREFIX = "management:metrics:exporter:cloudfoundry";
        public const string SPRING_APPLICATION_PREFIX = "spring:application";
        public const string FORWARDER_NAME = "metrics-forwarder";
        public const string ENDPOINT_KEY = "endpoint";
        public const string ACCESS_KEY = "access_key";

        public const int DEFAULT_TIMEOUT = 3;
        public const int DEFAULT_RATE = 60000;

        public CloudFoundryForwarderOptions()
            : base()
        {
        }

        public CloudFoundryForwarderOptions(IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var section = config.GetSection(CONFIG_PREFIX);
            if (section != null)
            {
                section.Bind(this);
            }

            section = config.GetSection(CloudFoundryApplicationOptions.CONFIGURATION_PREFIX);
            if (section != null)
            {
                CloudFoundryApplicationOptions appOptions = new CloudFoundryApplicationOptions(section);
                if (string.IsNullOrEmpty(ApplicationId))
                {
                    ApplicationId = appOptions.ApplicationId;
                }

                if (string.IsNullOrEmpty(InstanceId))
                {
                    InstanceId = appOptions.InstanceId;
                }

                if (string.IsNullOrEmpty(InstanceIndex))
                {
                    InstanceIndex = appOptions.InstanceIndex.ToString();
                }
            }

            section = config.GetSection(CloudFoundryServicesOptions.CONFIGURATION_PREFIX);
            if (section != null)
            {
                CloudFoundryServicesOptions servOptions = new CloudFoundryServicesOptions(section);
                if (servOptions.Services.TryGetValue(FORWARDER_NAME, out Service[] services))
                {
                    ConfigureServiceCredentials(services[0].Credentials);
                }
            }
        }

        public string Endpoint { get; set; }

        public string AccessToken { get; set; }

        public int RateMilli { get; set; } = DEFAULT_RATE;

        public bool ValidateCertificates { get; set; } = true;

        public int TimeoutSeconds { get; set; } = DEFAULT_TIMEOUT;

        public string ApplicationId { get; set; }

        public string InstanceId { get; set; }

        public string InstanceIndex { get; set; }

        public bool MicrometerMetricWriter { get; set; } = false;

        private void ConfigureServiceCredentials(Dictionary<string, Credential> credentials)
        {
            if (string.IsNullOrEmpty(Endpoint) && credentials.TryGetValue(ENDPOINT_KEY, out Credential endpoint))
            {
                Endpoint = endpoint.Value;
            }

            if (string.IsNullOrEmpty(AccessToken) && credentials.TryGetValue(ACCESS_KEY, out Credential token))
            {
                AccessToken = token.Value;
            }
        }
    }
}
