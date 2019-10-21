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

namespace Steeltoe.Management.Endpoint
{
    [Obsolete("Use ManagementEndpointOptions instead")]
    public class ManagementOptions : IManagementOptions
    {
        private const string DEFAULT_PATH = "/";
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints";
        private static ManagementOptions _instance;

        public ManagementOptions()
        {
            Path = DEFAULT_PATH;
            EndpointOptions = new List<IEndpointOptions>();
        }

        public ManagementOptions(IConfiguration config)
            : this()
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var section = config.GetSection(MANAGEMENT_INFO_PREFIX);
            if (section != null)
            {
                section.Bind(this);
            }
        }

        public bool? Enabled { get; set; }

        public bool? Sensitive { get; set; }

        public string Path { get; set; }

        public List<IEndpointOptions> EndpointOptions { get; set; }

        public static ManagementOptions GetInstance()
        {
            if (_instance == null)
            {
                _instance = new ManagementOptions();
            }

            return _instance;
        }

        public static ManagementOptions GetInstance(IConfiguration config)
        {
            if (_instance == null)
            {
                _instance = new ManagementOptions(config);
            }

            return _instance;
        }

        public static void SetInstance(ManagementOptions mgmtOptions)
        {
            _instance = mgmtOptions;
        }

        public static void Reset()
        {
            _instance = null;
        }
    }
}
