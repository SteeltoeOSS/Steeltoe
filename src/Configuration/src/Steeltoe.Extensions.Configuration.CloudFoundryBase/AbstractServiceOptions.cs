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
using System;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Configuration.CloudFoundry
{
    public abstract class AbstractServiceOptions
    {
        public const string CONFIGURATION_PREFIX = "vcap:services";

        public string Name { get; set; }

        public string Label { get; set; }

        public List<string> Tags { get; set; }

        public string Plan { get; set; }

        public void Bind(IConfiguration configuration, string serviceName)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException(nameof(serviceName));
            }

            var services = configuration.GetSection(CONFIGURATION_PREFIX);
            var section = FindServiceSection(services, serviceName);

            if (section != null)
            {
                section.Bind(this);
            }
        }

        internal IConfigurationSection FindServiceSection(IConfigurationSection section, string serviceName)
        {
            var children = section.GetChildren();
            foreach (var child in children)
            {
                string name = child.GetValue<string>("name");
                if (serviceName == name)
                {
                    return child;
                }
            }

            foreach (var child in children)
            {
                var result = FindServiceSection(child, serviceName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
