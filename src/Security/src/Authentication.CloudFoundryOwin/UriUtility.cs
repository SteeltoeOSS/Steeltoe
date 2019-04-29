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

using Microsoft.Owin;
using Microsoft.Owin.Infrastructure;
using System;

namespace Steeltoe.Security.Authentication.CloudFoundry.Owin
{
    internal static class UriUtility
    {
        /// <summary>
        /// Determine full redirect uri to send user to auth server, include a valid return path
        /// </summary>
        /// <param name="options">Auth configuration</param>
        /// <param name="request">HTTP Request information, for generating a valid return paht</param>
        /// <returns>A URL with enough info for the auth server to identify the app and return the user to the right location after auth</returns>
        internal static string CalculateFullRedirectUri(OpenIdConnectOptions options, IOwinRequest request)
        {
            var uri = options.AuthDomain + CloudFoundryDefaults.AuthorizationUri;

            var queryString = WebUtilities.AddQueryString(uri, CloudFoundryDefaults.ParamsClientId, options.ClientId);
            queryString = WebUtilities.AddQueryString(queryString, CloudFoundryDefaults.ParamsResponseType, "code");
            queryString = WebUtilities.AddQueryString(queryString, CloudFoundryDefaults.ParamsScope, $"{Constants.ScopeOpenID} {options.AdditionalScopes}");
            queryString = WebUtilities.AddQueryString(queryString, CloudFoundryDefaults.ParamsRedirectUri, DetermineRedirectUri(options, request));

            return queryString;
        }

        private static string DetermineRedirectUri(OpenIdConnectOptions options, IOwinRequest request)
        {
            return request.Scheme +
                Uri.SchemeDelimiter +
                request.Host +
                options.CallbackPath;
        }
    }
}
