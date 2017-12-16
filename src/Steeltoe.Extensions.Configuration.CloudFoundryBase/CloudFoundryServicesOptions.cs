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
using Steeltoe.Common.Options;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Configuration.CloudFoundry
{
    public class CloudFoundryServicesOptions : AbstractOptions
    {
        public const string CONFIGURATION_PREFIX = "vcap";

        public CloudFoundryServicesOptions()
        {
        }

        public CloudFoundryServicesOptions(IConfigurationRoot root)
            : base(root, CONFIGURATION_PREFIX)
        {
        }

        public CloudFoundryServicesOptions(IConfiguration config)
            : base(config)
        {
        }

        public Dictionary<string, Service[]> Services { get; set; } = new Dictionary<string, Service[]>();

        public IList<Service> ServicesList
        {
            get
            {
                List<Service> results = new List<Service>();
                if (Services != null)
                {
                    foreach (KeyValuePair<string, Service[]> kvp in Services)
                    {
                        results.AddRange(kvp.Value);
                    }
                }

                return results;
            }
        }
    }
}
