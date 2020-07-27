// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Autofac.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.Connector.OAuth;
using Steeltoe.CloudFoundry.Connector.Services;
using System;

namespace Steeltoe.CloudFoundry.ConnectorAutofac
{
    public static class OAuthContainerBuilderExtensions
    {
        /// <summary>
        /// Adds SsoServiceInfo (as IOptions of type OAuthServiceOptions) to your Autofac Container
        /// </summary>
        /// <param name="container">Your Autofac Container Builder</param>
        /// <param name="config">Application configuration</param>
        /// <param name="serviceName">Cloud Foundry service name binding</param>
        /// <returns>the RegistrationBuilder for (optional) additional configuration</returns>
        public static IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle> RegisterOAuthServiceOptions(this ContainerBuilder container, IConfiguration config, string serviceName = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var info = serviceName == null
                ? config.GetSingletonServiceInfo<SsoServiceInfo>()
                : config.GetRequiredServiceInfo<SsoServiceInfo>(serviceName);

            var oauthConfig = new OAuthConnectorOptions(config);
            var factory = new OAuthConnectorFactory(info, oauthConfig);

            return container.Register(c => factory.Create(null)).As<IOptions<OAuthServiceOptions>>();
        }
    }
}
