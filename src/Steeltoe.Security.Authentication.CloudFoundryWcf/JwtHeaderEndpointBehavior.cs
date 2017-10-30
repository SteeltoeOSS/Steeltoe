// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System;
using System.Configuration;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf
{
    public class JwtHeaderEndpointBehavior : BehaviorExtensionElement, IEndpointBehavior
    {
        const string SSOPropertyName = "ssoName";

        [ConfigurationProperty(SSOPropertyName)]
        public string SsoName 
        {
            get
            {
                return (string)base[SSOPropertyName];
            }
            set
            {
                base[SSOPropertyName] = value;
            }
        }

        public override Type BehaviorType
        {
            get { return typeof(JwtHeaderEndpointBehavior); }
        }

        protected override object CreateBehavior()
        {
            // Create the  endpoint behavior that will insert the message
            // inspector into the client runtime
            return new JwtHeaderEndpointBehavior();
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(new JwtHeaderMessageInspector());

        }

        public void AddBindingParameters(ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
        {

        }


        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.EndpointDispatcher endpointDispatcher)
        {
            
        }

        public void Validate(ServiceEndpoint endpoint)
        {
            
        }
    }


    

}
