// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using RichardSzalay.MockHttp;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Management.Endpoint.Test.Actuators.CloudFoundry;

internal static class CloudControllerPermissionsMock
{
    internal static DelegateToMockHttpClientHandler GetHttpMessageHandler()
    {
        var httpClientHandler = new DelegateToMockHttpClientHandler();

        httpClientHandler.Mock.When(HttpMethod.Get, "https://example.api.com/v2/apps/unavailable/permissions")
            .Respond(HttpStatusCode.ServiceUnavailable, "application/json", "{}");

        httpClientHandler.Mock.When(HttpMethod.Get, "https://example.api.com/v2/apps/not-found/permissions")
            .Respond(HttpStatusCode.NotFound, "application/json", "{}");

        httpClientHandler.Mock.When(HttpMethod.Get, "https://example.api.com/v2/apps/unauthorized/permissions")
            .Respond(HttpStatusCode.Unauthorized, "application/json", "{}");

        httpClientHandler.Mock.When(HttpMethod.Get, "https://example.api.com/v2/apps/forbidden/permissions")
            .Respond(HttpStatusCode.Forbidden, "application/json", "{}");

        httpClientHandler.Mock.When(HttpMethod.Get, "https://example.api.com/v2/apps/timeout/permissions")
            .Throw(new OperationCanceledException(null, new TimeoutException()));

        httpClientHandler.Mock.When(HttpMethod.Get, "https://example.api.com/v2/apps/exception/permissions")
            .Throw(new HttpRequestException(HttpRequestError.NameResolutionError));

        httpClientHandler.Mock.When(HttpMethod.Get, "https://example.api.com/v2/apps/no_sensitive_data/permissions").Respond(HttpStatusCode.OK,
            "application/json", """{"read_sensitive_data": false, "read_basic_data": true}""");

        httpClientHandler.Mock.When(HttpMethod.Get, "https://example.api.com/v2/apps/success/permissions").Respond(HttpStatusCode.OK, "application/json",
            """{"read_sensitive_data": true, "read_basic_data": true}""");

        return httpClientHandler;
    }
}
