// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            if (request.Properties.TryGetValue(HttpRequestMessageProperty.Name, out var httpRequestMessageObject))
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
#pragma warning disable S125 // Sections of code should not be commented out
                            // HttpResponseMessageProperty httpResponse;
                            // if (reply.Properties.ContainsKey(HttpResponseMessageProperty.Name))
                            // {
                            //    httpResponse = reply.Properties[HttpResponseMessageProperty.Name] as HttpResponseMessageProperty;
                            // }
                            // could get here refreshed token
        }
#pragma warning restore S125 // Sections of code should not be commented out

        private string GetAccessToken()
        {
            var tokenResolver = new CloudFoundryClientTokenResolver(_options, _httpClient);

            try
            {
               var accessToken = tokenResolver.GetAccessToken().GetAwaiter().GetResult();

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
