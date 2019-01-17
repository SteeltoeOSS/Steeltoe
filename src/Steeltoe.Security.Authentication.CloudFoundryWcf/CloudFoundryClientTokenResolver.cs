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

using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf
{
    /// <summary>
    /// Get JWTs for WCF Clients
    /// </summary>
    public class CloudFoundryClientTokenResolver
    {
        public CloudFoundryOptions Options { get; internal protected set; }

        private readonly ILogger<CloudFoundryClientTokenResolver> _logger;
        private readonly TokenExchanger _tokenExchanger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudFoundryClientTokenResolver"/> class.
        /// This class can be used to get access tokens from an OAuth server
        /// </summary>
        /// <param name="options">Con</param>
        /// <param name="httpClient">For interacting with the OAuth server. A new instance will be created if not provided.</param>
        public CloudFoundryClientTokenResolver(CloudFoundryOptions options, HttpClient httpClient = null)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options), "Options are required");
            _tokenExchanger = new TokenExchanger(options, httpClient, options.LoggerFactory?.CreateLogger<TokenExchanger>());
            _logger = Options.LoggerFactory?.CreateLogger<CloudFoundryClientTokenResolver>();
        }

        /// <summary>
        /// Get an access token using the client_credentials grant and the application's oauth credentials
        /// </summary>
        /// <returns>An access token</returns>
        public virtual async Task<string> GetAccessToken()
        {
            HttpResponseMessage response = await _tokenExchanger.GetAccessTokenWithClientCredentials(Options.AuthorizationUrl + Options.AccessTokenEndpoint);

            if (response.IsSuccessStatusCode)
            {
                _logger?.LogTrace("Successfully retrieved access token");

                var resp = await response.Content.ReadAsStringAsync();
                var payload = JObject.Parse(resp);

                return payload.Value<string>("access_token");
            }
            else
            {
                _logger?.LogError("Failed to retrieve access token with HTTP Status: {HttpStatus}", response.StatusCode);
                _logger?.LogWarning("Access token retrieval failure response: {Message}", response.Content.ReadAsStringAsync());
                var error = "OAuth token endpoint failure: " + await Display(response);
                throw new Exception(error);
            }
        }

        private static async Task<string> Display(HttpResponseMessage response)
        {
            var output = new StringBuilder();
            output.Append("Status: " + response.StatusCode + ";");
            output.Append("Headers: " + response.Headers.ToString() + ";");
            output.Append("Body: " + await response.Content.ReadAsStringAsync() + ";");
            return output.ToString();
        }
    }
}