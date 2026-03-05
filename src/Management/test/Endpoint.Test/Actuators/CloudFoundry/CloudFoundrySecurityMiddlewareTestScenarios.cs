// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Messages = Steeltoe.Management.Endpoint.Actuators.CloudFoundry.PermissionsProvider.Messages;

namespace Steeltoe.Management.Endpoint.Test.Actuators.CloudFoundry;

internal sealed class CloudFoundrySecurityMiddlewareTestScenarios : TheoryData<string, HttpStatusCode?, string?, string[], bool>
{
    private const string FullPermissionsLog =
        "INFO System.Net.Http.HttpClient.CloudFoundrySecurity.ClientHandler: Sending HTTP request GET https://example.api.com/v3/apps/full-permissions/permissions";

    private static readonly string PermissionsCheckForbiddenLog =
        $"INFO {typeof(PermissionsProvider)}: Cloud Foundry returned status Forbidden while obtaining permissions from https://example.api.com/v3/apps/forbidden/permissions.";

    private static readonly string PermissionsCheckUnauthorizedLog =
        $"INFO {typeof(PermissionsProvider)}: Cloud Foundry returned status Unauthorized while obtaining permissions from https://example.api.com/v3/apps/unauthorized/permissions.";

    private static readonly string PermissionsCheckNotFoundLog =
        $"INFO {typeof(PermissionsProvider)}: Cloud Foundry returned status NotFound while obtaining permissions from https://example.api.com/v3/apps/not-found/permissions.";

    private static readonly string MiddlewareForbiddenLog =
        $"FAIL {typeof(CloudFoundrySecurityMiddleware)}: Actuator security error with status Forbidden: '{Messages.AccessDenied}'.";

    private static readonly string MiddlewareBrokenResponseLog =
        $"FAIL {typeof(CloudFoundrySecurityMiddleware)}: Actuator security error with status BadGateway: '{Messages.CloudFoundryBrokenResponse}'.";

    private static readonly string MiddlewareExceptionLog =
        $"FAIL {typeof(CloudFoundrySecurityMiddleware)}: Actuator security error with status ServiceUnavailable: " +
        $"'Exception of type '{typeof(HttpRequestException)}' with error '{nameof(HttpRequestError.NameResolutionError)}' was thrown'.";

    private static readonly string MiddlewareTimeoutLog =
        $"FAIL {typeof(CloudFoundrySecurityMiddleware)}: Actuator security error with status ServiceUnavailable: '{Messages.CloudFoundryTimeout}'.";

    private static readonly string MiddlewareUnauthorizedLog =
        $"FAIL {typeof(CloudFoundrySecurityMiddleware)}: Actuator security error with status Unauthorized: '{Messages.InvalidToken}'.";

    private static readonly string MiddlewareUnavailableLog =
        $"FAIL {typeof(CloudFoundrySecurityMiddleware)}: Actuator security error with status ServiceUnavailable: '{Messages.CloudFoundryNotReachable}'.";

    public CloudFoundrySecurityMiddlewareTestScenarios()
    {
        Add("exception", HttpStatusCode.ServiceUnavailable,
            $"Exception of type '{typeof(HttpRequestException)}' with error '{nameof(HttpRequestError.NameResolutionError)}' was thrown",
            [MiddlewareExceptionLog], true);

        Add("exception", HttpStatusCode.OK,
            $"Exception of type '{typeof(HttpRequestException)}' with error '{nameof(HttpRequestError.NameResolutionError)}' was thrown",
            [MiddlewareExceptionLog], false);

        Add("forbidden", HttpStatusCode.Forbidden, Messages.AccessDenied, [
            PermissionsCheckForbiddenLog,
            MiddlewareForbiddenLog
        ], true);

        Add("forbidden", HttpStatusCode.Forbidden, Messages.AccessDenied, [
            PermissionsCheckForbiddenLog,
            MiddlewareForbiddenLog
        ], false);

        Add("broken-response", HttpStatusCode.BadGateway, Messages.CloudFoundryBrokenResponse, [MiddlewareBrokenResponseLog], true);

        Add("broken-response", HttpStatusCode.OK, Messages.CloudFoundryBrokenResponse, [MiddlewareBrokenResponseLog], false);

        Add("no-permissions", HttpStatusCode.Forbidden, Messages.AccessDenied, [MiddlewareForbiddenLog], true);

        Add("no-permissions", HttpStatusCode.Forbidden, Messages.AccessDenied, [MiddlewareForbiddenLog], false);

        Add("restricted-permissions", HttpStatusCode.OK, null, [MiddlewareForbiddenLog], true);

        Add("full-permissions", HttpStatusCode.OK, null, [FullPermissionsLog], true);

        Add("not-found", HttpStatusCode.Unauthorized, Messages.InvalidToken, [
            PermissionsCheckNotFoundLog,
            MiddlewareUnauthorizedLog
        ], true);

        Add("not-found", HttpStatusCode.Unauthorized, Messages.InvalidToken, [
            PermissionsCheckNotFoundLog,
            MiddlewareUnauthorizedLog
        ], false);

        Add("timeout", HttpStatusCode.ServiceUnavailable, Messages.CloudFoundryTimeout, [MiddlewareTimeoutLog], true);

        Add("timeout", HttpStatusCode.OK, Messages.CloudFoundryTimeout, [MiddlewareTimeoutLog], false);

        Add("unauthorized", HttpStatusCode.Unauthorized, Messages.InvalidToken, [
            PermissionsCheckUnauthorizedLog,
            MiddlewareUnauthorizedLog
        ], true);

        Add("unauthorized", HttpStatusCode.Unauthorized, Messages.InvalidToken, [
            PermissionsCheckUnauthorizedLog,
            MiddlewareUnauthorizedLog
        ], false);

        Add("unavailable", HttpStatusCode.ServiceUnavailable, Messages.CloudFoundryNotReachable, [MiddlewareUnavailableLog], true);

        Add("unavailable", HttpStatusCode.OK, Messages.CloudFoundryNotReachable, [MiddlewareUnavailableLog], false);
    }
}
