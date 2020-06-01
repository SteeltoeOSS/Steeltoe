// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Security;

namespace Steeltoe.Management.Endpoint.Refresh
{
    public class RefreshEndpointOptions : AbstractEndpointOptions, IRefreshOptions
    {
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:refresh";

        public RefreshEndpointOptions()
            : base()
        {
            Id = "refresh";
            RequiredPermissions = Permissions.RESTRICTED;
        }

        public RefreshEndpointOptions(IConfiguration config)
            : base(MANAGEMENT_INFO_PREFIX, config)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = "refresh";
            }

            if (RequiredPermissions == Permissions.UNDEFINED)
            {
                RequiredPermissions = Permissions.RESTRICTED;
            }
        }
    }
}
