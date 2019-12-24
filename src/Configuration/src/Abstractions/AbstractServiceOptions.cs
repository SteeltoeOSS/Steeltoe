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
using Steeltoe.Common.Options;
using System;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Configuration
{
    public abstract class AbstractServiceOptions : AbstractOptions, IServicesInfo
    {
        public virtual string CONFIGURATION_PREFIX { get; protected set; } = "services";

        // This constructor is for use with IOptions
        protected AbstractServiceOptions()
        {
        }

        protected AbstractServiceOptions(IConfigurationRoot root, string sectionPrefix = "")
            : base(root, sectionPrefix)
        {
        }

        protected AbstractServiceOptions(IConfiguration config, string sectionPrefix = "")
            : base(config, sectionPrefix)
        {
        }

        /// <summary>
        /// Gets or sets the name of the service instance
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a label describing the type of service
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the plan level at which the service is provisoned
        /// </summary>
        public IEnumerable<string> Tags { get; set; }

        /// <summary>
        /// Gets or sets a list of tags describing the service
        /// </summary>
        public string Plan { get; set; }

        public Dictionary<string, IEnumerable<Service>> Services { get; set; } = new Dictionary<string, IEnumerable<Service>>();

        public IEnumerable<Service> GetServicesList()
        {
            var results = new List<Service>();
            if (Services != null)
            {
                foreach (var kvp in Services)
                {
                    results.AddRange(kvp.Value);
                }
            }

            return results;
        }

        public IEnumerable<Service> GetInstancesOfType(string serviceType)
        {
            Services.TryGetValue(serviceType, out var services);
            return services ?? new List<Service>();
        }

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
                var name = child.GetValue<string>("name");
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
