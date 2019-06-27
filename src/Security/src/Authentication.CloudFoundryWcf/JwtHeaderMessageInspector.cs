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

using System;
using System.Net.Http;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Threading.Tasks;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf
{
    public class JwtHeaderMessageInspector : IClientMessageInspector
    {
        private readonly HttpClient _httpClient;
        private readonly CloudFoundryOptions _options;
        private string _token;
        private string _userToken;

        public JwtHeaderMessageInspector(CloudFoundryOptions options, string userToken, HttpClient httpClient = null)
        {
            _options = options;
            _userToken = userToken;
            _httpClient = httpClient;
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            if (_options.ForwardUserCredentials)
            {
                _token = _userToken;
            }
            else
            {
                if (_token == null)
                {
                    _token = GetAccessToken();
                }
            }

            HttpRequestMessageProperty httpRequestMessage;
            if (request.Properties.TryGetValue(HttpRequestMessageProperty.Name, out object httpRequestMessageObject))
            {
                httpRequestMessage = httpRequestMessageObject as HttpRequestMessageProperty;
            }
            else
            {
                httpRequestMessage = new HttpRequestMessageProperty();
                request.Properties.Add(HttpRequestMessageProperty.Name, httpRequestMessage);
            }

            if (httpRequestMessage != null)
            {
                httpRequestMessage.Headers.Add("Authorization", string.Format("Bearer {0}", _token));
            }

            return null;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            HttpResponseMessageProperty httpResponse;
            if (reply.Properties.ContainsKey(HttpResponseMessageProperty.Name))
            {
                httpResponse = reply.Properties[HttpResponseMessageProperty.Name] as HttpResponseMessageProperty;
            }

            // could get here refreshed token
        }

        private string GetAccessToken()
        {
            CloudFoundryClientTokenResolver tokenResolver = new CloudFoundryClientTokenResolver(_options, _httpClient);

            try
            {
               string accessToken = tokenResolver.GetAccessToken().GetAwaiter().GetResult();

               return accessToken;
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message + ex.StackTrace);
                throw;
            }
        }
    }
}
