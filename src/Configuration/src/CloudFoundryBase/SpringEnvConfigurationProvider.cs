// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;

namespace Steeltoe.Extensions.Configuration.CloudFoundry
{
    public class SpringEnvConfigurationProvider : ConfigurationProvider
    {

        public SpringEnvConfigurationProvider()
        {
        }

        public override void Load()
        {
            var springAppJson = "test"; //TODO: fix configuration 

            if (!string.IsNullOrEmpty(springAppJson))
            {
                var memStream = CloudFoundryConfigurationProvider.GetMemoryStream(springAppJson);
                var builder = new ConfigurationBuilder();
                builder.Add(new JsonStreamConfigurationSource(memStream));
                var servicesData = builder.Build();
                if (servicesData != null)
                {
                    foreach (var child in servicesData.GetChildren())
                    {
                        if (child.Key.Contains(".") && child.Value != null)
                        {
                            var nk = child.Key.Replace('.', ':');
                            Data[nk] = child.Value;
                        }

                        Expand(child);
                    }
                }
            }
        }

        private void Expand(IConfigurationSection section)
        {
            foreach (var child in section.GetChildren())
            {
                if (child.Key.Contains(".") && child.Value != null)
                {
                    var nk = child.Path.Replace('.', ':');
                    Data[nk] = child.Value;
                }

                Expand(child);
            }
        }
    }
}
