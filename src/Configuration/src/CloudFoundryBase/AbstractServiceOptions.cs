// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
