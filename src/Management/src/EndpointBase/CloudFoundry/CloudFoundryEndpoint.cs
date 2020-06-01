// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Hypermedia;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Endpoint.CloudFoundry
{
#pragma warning disable CS0618 // Type or member is obsolete
    public class CloudFoundryEndpoint : AbstractEndpoint<Links, string>
    {
        private readonly ILogger<CloudFoundryEndpoint> _logger;
        private readonly IManagementOptions _mgmtOption;

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

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
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
                HypermediaService hypermediaService = new HypermediaService(_mgmtOption, options, _logger);
                return hypermediaService.Invoke(baseUrl);
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
#pragma warning restore CS0618 // Type or member is obsolete