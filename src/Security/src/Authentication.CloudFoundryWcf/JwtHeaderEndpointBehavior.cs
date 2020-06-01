// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Configuration;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf
{
    public class JwtHeaderEndpointBehavior : BehaviorExtensionElement, IEndpointBehavior
    {
        private const string SSOPropertyName = "ssoName";
        private CloudFoundryOptions _options;
        private string _userToken;

        public JwtHeaderEndpointBehavior(CloudFoundryOptions options, string userToken = null)
        {
            _options = options;
            _userToken = userToken;
        }

        [ConfigurationProperty(SSOPropertyName)]
        public string SsoName
        {
            get
            {
                return (string)this[SSOPropertyName];
            }

            set
            {
                this[SSOPropertyName] = value;
            }
        }

        public override Type BehaviorType
        {
            get { return typeof(JwtHeaderEndpointBehavior); }
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(new JwtHeaderMessageInspector(_options, _userToken));
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
            // for future use
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            // for future use
        }

        public void Validate(ServiceEndpoint endpoint)
        {
            // for future use
        }

        protected override object CreateBehavior()
        {
            // Create the endpoint behavior that will insert the message inspector into the client runtime
            return new JwtHeaderEndpointBehavior(_options, _userToken);
        }
    }
}
