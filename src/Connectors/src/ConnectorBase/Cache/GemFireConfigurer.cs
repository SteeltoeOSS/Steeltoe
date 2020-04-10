// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Steeltoe.Connector.Services;
using System.Linq;

namespace Steeltoe.Connector.GemFire
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
