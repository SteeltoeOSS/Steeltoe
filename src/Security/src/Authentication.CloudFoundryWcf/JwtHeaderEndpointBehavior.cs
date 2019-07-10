﻿// Copyright 2017 the original author or authors.
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
