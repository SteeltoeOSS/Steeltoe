// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Runtime.InteropServices;
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
            var response = await _tokenExchanger.GetAccessTokenWithClientCredentials(Options.AuthorizationUrl + Options.AccessTokenEndpoint).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger?.LogTrace("Successfully retrieved access token");

                var resp = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var payload = JObject.Parse(resp);

                return payload.Value<string>("access_token");
            }
            else
            {
                _logger?.LogError("Failed to retrieve access token with HTTP Status: {HttpStatus}", response.StatusCode);
                _logger?.LogWarning("Access token retrieval failure response: {Message}", response.Content.ReadAsStringAsync());
                var error = "OAuth token endpoint failure: " + await Display(response).ConfigureAwait(false);
                throw new ExternalException(error);
            }
        }

        private static async Task<string> Display(HttpResponseMessage response)
        {
            var output = new StringBuilder();
            output.Append("Status: " + response.StatusCode + ";");
            output.Append("Headers: " + response.Headers.ToString() + ";");
            output.Append("Body: " + await response.Content.ReadAsStringAsync().ConfigureAwait(false) + ";");
            return output.ToString();
        }
    }
}