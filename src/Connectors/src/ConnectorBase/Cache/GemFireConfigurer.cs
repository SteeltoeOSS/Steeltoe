// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector.Services;
using System.Linq;

namespace Steeltoe.CloudFoundry.Connector.GemFire
{
    public class GemFireConfigurer
    {
        private ILogger _logger;

        /// <summary>
        /// Apply VCAP_SERVICES data to GemFire defaults and user config
        /// </summary>
        /// <param name="serviceInfo">Service Info from VCAP_SERVICES</param>
        /// <param name="gemFireOptions">User-configured GemFire options</param>
        /// <param name="logger">For logging if the binding isn't working</param>
        /// <returns>Combination of user settings and platform credentials</returns>
        public GemFireConnectorOptions Configure(GemFireServiceInfo serviceInfo, GemFireConnectorOptions gemFireOptions, ILogger logger = null)
        {
            _logger = logger;
            UpdateOptions(serviceInfo, gemFireOptions);
            return gemFireOptions;
        }

        internal void UpdateOptions(GemFireServiceInfo serviceInfo, GemFireConnectorOptions gemFireOptions)
        {
            // if there is no service info or the service info has no locators, just move on
            if (serviceInfo == null || !serviceInfo.Locators.Any())
            {
                _logger?.LogTrace("Found either no service info at all, or no locators within");
                return;
            }

            // use only the locators provided by vcap_services
            gemFireOptions.Locators.Clear();
            gemFireOptions.Locators.AddRange(serviceInfo.Locators);
            _logger?.LogTrace("GemFire is operating with {LocatorCount} locators", gemFireOptions.Locators.Count);

            // find and apply the credentials with the developer role
            var developerUser = serviceInfo.Users.First(r => r.Roles.Contains("developer"));
            gemFireOptions.Username = developerUser.Username;
            gemFireOptions.Password = developerUser.Password;
            _logger?.LogTrace("GemFire will connect with username {GemFireUsername}", gemFireOptions.Username);
        }
    }
}
