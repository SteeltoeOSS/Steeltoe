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
using Steeltoe.Management.Endpoint.Security;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint
{
    public class Exposure
    {
        private const string EXPOSURE_PREFIX = "management:endpoints:actuator:exposure";
        private static readonly List<string> DEFAULT_INCLUDE = new List<string> { "health", "info" };

        public Exposure()
        {
            Include = DEFAULT_INCLUDE;
        }

        public Exposure(IConfiguration config)
        {
            var section = config.GetSection(EXPOSURE_PREFIX);
            if (section != null)
            {
                section.Bind(this);
            }

            if (Include == null && Exclude == null)
            {
                Include = DEFAULT_INCLUDE;
            }
        }

        public List<string> Include { get; set; }

        public List<string> Exclude { get; set; }
    }
}
