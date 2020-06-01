// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.EndpointOwin
{
    public static class ManagementOptions
    {
        private static IEnumerable<IManagementOptions> _mgmtOptions;

        public static IEnumerable<IManagementOptions> Get()
        {
            if (_mgmtOptions == null)
            {
                throw new InvalidOperationException("Management Options not configured");
            }

            return _mgmtOptions;
        }

        public static IEnumerable<IManagementOptions> Get(IConfiguration config)
        {
            if (_mgmtOptions == null)
            {
                _mgmtOptions = new List<IManagementOptions>
                {
                     new CloudFoundryManagementOptions(config),
                     new ActuatorManagementOptions(config)
                };
            }

            return _mgmtOptions;
        }

        /// <summary>
        /// For Testing
        /// </summary>
        internal static void Clear()
        {
            _mgmtOptions = null;
        }
    }
}
