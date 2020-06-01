// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
