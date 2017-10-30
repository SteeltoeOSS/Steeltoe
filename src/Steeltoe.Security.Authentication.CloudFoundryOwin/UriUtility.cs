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

using Microsoft.Owin.Infrastructure;
using System;


namespace Steeltoe.Security.Authentication.CloudFoundryOwin
{
    internal static class UriUtility
    {
        internal static String CalculateFullRedirectUri(OpenIDConnectOptions options)
        {
            var uri = options.AuthDomain + "/" + Constants.EndPointOAuthAuthorize;

            var queryString = WebUtilities.AddQueryString(uri, Constants.ParamsClientID, options.ClientID);
            queryString = WebUtilities.AddQueryString(queryString, Constants.ParamsResponseType, "code");
            queryString = WebUtilities.AddQueryString(queryString, Constants.ParamsScope, Constants.ScopeOpenID);
            queryString = WebUtilities.AddQueryString(queryString, Constants.ParamsRedirectUri, DetermineRedirectUri(options));

            return queryString;
        }

        private static string DetermineRedirectUri(OpenIDConnectOptions options)
        {
            // TODO: determine the right host name and port.
            var uri = "https://" + options.AppHost;
            if (options.AppPort > 0 && options.AppPort != 443)
            {
                uri = uri + ":" + options.AppPort.ToString();
            }
            return uri + options.CallbackPath;
        }

    }
}
