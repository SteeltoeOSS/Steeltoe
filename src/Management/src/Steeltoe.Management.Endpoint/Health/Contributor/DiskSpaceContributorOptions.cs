//
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

using System;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.Health.Contributor
{
    public class DiskSpaceContributorOptions
    {
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:health:diskspace";
        private const long DEFAULT_THRESHOLD = 10 * 1024 * 1024;
        public DiskSpaceContributorOptions() 
        {
            Path = ".";
            Threshold = DEFAULT_THRESHOLD;
        }

        public DiskSpaceContributorOptions(IConfiguration config) 
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
            if (string.IsNullOrEmpty(Path))
            {
                Path = ".";
            }
            if (Threshold == -1)
            {
                Threshold = DEFAULT_THRESHOLD;
            }
        }

        public string Path { get; set; }
        public long Threshold { get; set; } = -1;

    }
}
