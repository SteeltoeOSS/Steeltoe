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
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf
{

    public class JwtHeaderMessageInspector : IClientMessageInspector
    {
        private  string _token;

        public JwtHeaderMessageInspector()
        {
        }

        public object BeforeSendRequest(ref System.ServiceModel.Channels.Message request, System.ServiceModel.IClientChannel channel)
        {
        
            if (_token == null)
                _token = GetAccessToken();
           

            HttpRequestMessageProperty httpRequestMessage;
            object httpRequestMessageObject;
            if (request.Properties.TryGetValue(HttpRequestMessageProperty.Name, out httpRequestMessageObject))
            {
                httpRequestMessage = httpRequestMessageObject as HttpRequestMessageProperty;
            }
            else
            {
                httpRequestMessage = new HttpRequestMessageProperty();
                request.Properties.Add(HttpRequestMessageProperty.Name, httpRequestMessage);
            }

            if (httpRequestMessage != null)
                httpRequestMessage.Headers.Add("Authorization", string.Format("Bearer {0}", _token));
         
            return null;
        }


        public void AfterReceiveReply(ref System.ServiceModel.Channels.Message reply, object correlationState)
        {
            HttpResponseMessageProperty httpResponse;
            if (reply.Properties.ContainsKey(HttpResponseMessageProperty.Name))
            {
                httpResponse = reply.Properties[HttpResponseMessageProperty.Name] as HttpResponseMessageProperty;
            }
            //could get here refreshed token
            
        }


        private  string  GetAccessToken()
        {


			CloudFoundryOptions options = new CloudFoundryOptions();
			
			CloudFoundryClientTokenResolver tokenResolver = new CloudFoundryClientTokenResolver(options);

            try
            {
               string accessToken = tokenResolver.GetAccessToken().GetAwaiter().GetResult();

               return accessToken;

            }
            catch (Exception ex)
            {
               
                Console.Out.WriteLine(ex.Message + ex.StackTrace);
                throw new Exception("Cannont obtain the Access Token" + ex.Message);
            }

        }

    }
}
