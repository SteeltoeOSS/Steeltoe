// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
                request.PathBase +
                options.CallbackPath;
        }
    }
}
