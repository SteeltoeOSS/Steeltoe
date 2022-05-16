// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.Refresh
{
    public class RefreshEndpointOptions : AbstractEndpointOptions, IRefreshOptions
    {
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:refresh";
        private const bool RETURN_CONFIGURATION = true;

        public RefreshEndpointOptions()
        {
            Id = "refresh";
            RequiredPermissions = Permissions.RESTRICTED;
            _returnConfiguration = RETURN_CONFIGURATION;
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

            if (!_returnConfiguration.HasValue)
            {
                _returnConfiguration = RETURN_CONFIGURATION;
            }
        }

        private bool? _returnConfiguration;

        public bool ReturnConfiguration
        {
            get => _returnConfiguration ?? RETURN_CONFIGURATION;

            set
            {
                _returnConfiguration = value;
            }
        }
    }
}
