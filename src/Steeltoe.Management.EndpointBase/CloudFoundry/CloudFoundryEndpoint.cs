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

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Discovery;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Endpoint.CloudFoundry
{
    public class CloudFoundryEndpoint : AbstractEndpoint<Links, string>
    {
        private ILogger<CloudFoundryEndpoint> _logger;
        private IManagementOptions _mgmtOption;

        public CloudFoundryEndpoint(ICloudFoundryOptions options, IEnumerable<IManagementOptions> mgmtOptions, ILogger<CloudFoundryEndpoint> logger = null)
        : base(options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _mgmtOption = mgmtOptions?.OfType<CloudFoundryManagementOptions>().SingleOrDefault();

            if (_mgmtOption == null)
            {
                throw new ArgumentNullException(nameof(mgmtOptions));
            }

            _logger = logger;
        }

        [Obsolete]
        public CloudFoundryEndpoint(ICloudFoundryOptions options, ILogger<CloudFoundryEndpoint> logger = null)
            : base(options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _logger = logger;
        }

        protected new ICloudFoundryOptions Options => options as ICloudFoundryOptions;

        public override Links Invoke(string baseUrl)
        {
            if (_mgmtOption != null)
            {
                DiscoveryService discoveryService = new DiscoveryService(_mgmtOption, options, _logger);
                return discoveryService.Invoke(baseUrl);
            }

            // TODO: The below code will be removed in 3.0
            else
            {
                var endpointOptions = Options.Global.EndpointOptions;
                var links = new Links();

                if (!Options.Enabled.Value)
                {
                    return links;
                }

                foreach (var opt in endpointOptions)
                {
                    if (!opt.Enabled.Value)
                    {
                        continue;
                    }

                    if (opt == Options)
                    {
                        links._links.Add("self", new Link(baseUrl));
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(opt.Id) && !links._links.ContainsKey(opt.Id))
                        {
                            links._links.Add(opt.Id, new Link(baseUrl + "/" + opt.Id));
                        }
                        else if (links._links.ContainsKey(opt.Id))
                        {
                            _logger?.LogWarning("Duplicate endpoint id detected: {DuplicateEndpointId}", opt.Id);
                        }
                    }
                }

                return links;
            }
        }
    }
}
