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
using Steeltoe.Management.Endpoint.CloudFoundry;
using System;

namespace Steeltoe.Management.Endpoint.Discovery
{
    public class DiscoveryService
    {
        private ILogger _logger;
        private IManagementOptions _mgmtOptions;
        private IEndpointOptions _options;

        public DiscoveryService(IManagementOptions mgmtOptions, IEndpointOptions discoveryOptions, ILogger logger = null)
        {
            _logger = logger;
            _mgmtOptions = mgmtOptions ?? throw new ArgumentNullException(nameof(mgmtOptions));
            _options = discoveryOptions ?? throw new ArgumentNullException(nameof(discoveryOptions));
        }

        public Links Invoke(string baseUrl)
        {
            var endpointOptions = _mgmtOptions.EndpointOptions;
            var links = new Links();

            if (!_options.IsEnabled(_mgmtOptions))
            {
                return links;
            }

            foreach (var opt in endpointOptions)
            {
                if (!opt.IsEnabled(_mgmtOptions))
                {
                    continue;
                }

                if (opt == _options)
                {
                    links._links.Add("self", new Link(baseUrl));
                }
                else
                {
                    if (!string.IsNullOrEmpty(opt.Id) && !links._links.ContainsKey(opt.Id))
                    {
                        links._links.Add(opt.Id, new Link(baseUrl + "/" + opt.Path));
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
