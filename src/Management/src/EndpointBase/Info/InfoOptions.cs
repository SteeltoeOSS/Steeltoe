// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Security;
using System;

namespace Steeltoe.Management.Endpoint.Info
{
    [Obsolete("Use InfoEndpointOptions instead.")]
    public class InfoOptions : AbstractOptions, IInfoOptions
    {
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:info";

        public InfoOptions()
            : base()
        {
            Id = "info";
            RequiredPermissions = Permissions.RESTRICTED;
        }

        public InfoOptions(IConfiguration config)
            : base(MANAGEMENT_INFO_PREFIX, config)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = "info";
            }

            if (RequiredPermissions == Permissions.UNDEFINED)
            {
                RequiredPermissions = Permissions.RESTRICTED;
            }
        }
    }
}
