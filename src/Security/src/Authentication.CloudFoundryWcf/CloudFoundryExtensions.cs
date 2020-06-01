// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.Connector.Services;
using System;
using System.Net.Http;
using System.ServiceModel;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf
{
    public static class CloudFoundryExtensions
    {
        /// <summary>
        /// Adds the <see cref="JwtAuthorizationManager"/> to a <see cref="ServiceHost"/>
        /// </summary>
        /// <param name="serviceHost">Your service to be secured with JWT Auth</param>
        /// <param name="configuration">Your application configuration, including VCAP_SERVICES</param>
        /// <param name="httpClient">Provide your own http client for interacting with the security server</param>
        /// <param name="loggerFactory">For logging within the library</param>
        /// <returns>Your service</returns>
        public static ServiceHost AddJwtAuthorization(this ServiceHost serviceHost, IConfiguration configuration, HttpClient httpClient = null, LoggerFactory loggerFactory = null)
        {
            if (serviceHost == null)
            {
                throw new ArgumentNullException(nameof(serviceHost));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            // get options with defaults
            var cloudFoundryOptions = new CloudFoundryOptions(loggerFactory);

            // get and apply config from application
            var securitySection = configuration.GetSection(CloudFoundryDefaults.SECURITY_CLIENT_SECTION_PREFIX);
            securitySection.Bind(cloudFoundryOptions);

            // get and apply service binding info
            SsoServiceInfo info = configuration.GetSingletonServiceInfo<SsoServiceInfo>();
            CloudFoundryOptionsConfigurer.Configure(info, cloudFoundryOptions);

            var authManager = new JwtAuthorizationManager(cloudFoundryOptions);
            serviceHost.Authorization.ServiceAuthorizationManager = authManager;

            return serviceHost;
        }
    }
}
