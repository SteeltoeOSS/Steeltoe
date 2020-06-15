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

        protected new ICloudFoundryOptions Options => options as ICloudFoundryOptions;

        public override Links Invoke(string baseUrl)
        {
            var hypermediaService = new HypermediaService(_mgmtOption, options, _logger);
            return hypermediaService.Invoke(baseUrl);
        }
    }
}
