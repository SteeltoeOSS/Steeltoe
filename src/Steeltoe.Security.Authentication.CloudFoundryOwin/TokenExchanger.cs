// Copyright 2017 the original author or authors.
//
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

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Security.Authentication.CloudFoundry.Owin
{
    internal class TokenExchanger
    {
        internal static async Task<ClaimsIdentity> ExchangeCodeForToken(string code, OpenIDConnectOptions options)
        {
            string redirect_url = "https://" + options.AppHost;
            if (options.AppPort != 0)
            {
                redirect_url = redirect_url + ":" + options.AppPort + options.CallbackPath;
            }
            else
            {
                redirect_url = redirect_url + options.CallbackPath;
            }

            var hostName = options.AuthDomain;
            var pairs = new[]
            {
                new KeyValuePair<string, string>(Constants.ParamsClientID, options.ClientID),
                new KeyValuePair<string, string>(Constants.ParamsClientSecret, options.ClientSecret),
                new KeyValuePair<string, string>(Constants.ParamsGrantType, Constants.GrantTypeAuthorizationCode),
                new KeyValuePair<string, string>(Constants.ParamsResponseType, Constants.ResponseTypeIDToken),
                new KeyValuePair<string, string>(Constants.ParamsTokenFormat, Constants.TokenFormatOpaque),
                new KeyValuePair<string, string>(Constants.ParamsRedirectUri, redirect_url),
                new KeyValuePair<string, string>(Constants.ParamsCode, code)
            };
            var content = new FormUrlEncodedContent(pairs);
            var targetUrl = hostName + "/" + Constants.EndPointOAuthToken;
            Debug.WriteLine("About to submit request for token to : " + targetUrl);
            foreach (var item in pairs)
            {
                Debug.WriteLine(item.Key + ": " + item.Value);
            }

            using (var client = new HttpClient())
            {
                var byteArray = Encoding.ASCII.GetBytes(options.ClientID + ":" + options.ClientSecret);
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var response = await client.PostAsync(targetUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    string resultJson = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine("Got from token endpoint:" + resultJson);
                    dynamic data = JObject.Parse(resultJson);
                    string jwtToken = data.id_token;
                    Debug.WriteLine("Retrieved authorization token from IDP: " + jwtToken);
                    JwtSecurityToken securityToken = new JwtSecurityToken(jwtToken);
                    var claimsId = new ClaimsIdentity(options.SignInAsAuthenticationType);
                    string userName = securityToken.Claims.First(c => c.Type == "user_name").Value;
                    string email = securityToken.Claims.First(c => c.Type == "email").Value;
                    string userId = securityToken.Claims.First(c => c.Type == "user_id").Value;
                    foreach (var claim in securityToken.Claims)
                    {
                        Debug.WriteLine(claim.Type + " : " + claim.Value);

                        // claimsId.AddClaim(claim);
                    }

                    claimsId.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
                    claimsId.AddClaim(new Claim(ClaimTypes.Name, userName));
                    claimsId.AddClaim(new Claim(ClaimTypes.Email, email));
                    return claimsId;
                }
                else
                {
                    Debug.WriteLine("Failed call to exchange code for token : " + response.StatusCode);
                    Debug.WriteLine(response.ReasonPhrase);
                    string resultJson = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine(resultJson);
                    return null;
                }
            }
        }
    }
}
