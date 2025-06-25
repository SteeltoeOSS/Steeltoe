// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Messages = Steeltoe.Management.Endpoint.Actuators.CloudFoundry.PermissionsProvider.Messages;

namespace Steeltoe.Management.Endpoint.Test.Actuators.CloudFoundry;

internal sealed class CloudFoundrySecurityMiddlewareTestScenarios : TheoryData<string, HttpStatusCode?, string?, string[], bool>
{
    private const string SuccessLog =
        "INFO System.Net.Http.HttpClient.CloudFoundrySecurity.ClientHandler: Sending HTTP request GET https://example.api.com/v2/apps/success/permissions";

    private readonly string _permissionsCheckForbiddenLog =
        $"INFO {typeof(PermissionsProvider)}: Cloud Foundry returned status: Forbidden while obtaining permissions from: https://example.api.com/v2/apps/forbidden/permissions";

    private readonly string _permissionsCheckUnauthorizedLog =
        $"INFO {typeof(PermissionsProvider)}: Cloud Foundry returned status: Unauthorized while obtaining permissions from: https://example.api.com/v2/apps/unauthorized/permissions";

    private readonly string _permissionsCheckNotFoundLog =
        $"INFO {typeof(PermissionsProvider)}: Cloud Foundry returned status: NotFound while obtaining permissions from: https://example.api.com/v2/apps/not-found/permissions";

    private readonly string _middlewareForbiddenLog =
        $"FAIL {typeof(CloudFoundrySecurityMiddleware)}: Actuator Security Error: Forbidden - {Messages.AccessDenied}";

    private readonly string _middlewareExceptionLog =
        $"FAIL {typeof(CloudFoundrySecurityMiddleware)}: Actuator Security Error: ServiceUnavailable - Exception of type 'System.Net.Http.HttpRequestException' with error 'NameResolutionError' was thrown";

    private readonly string _middlewareTimeoutLog =
        $"FAIL {typeof(CloudFoundrySecurityMiddleware)}: Actuator Security Error: ServiceUnavailable - {Messages.CloudFoundryTimeout}";

    private readonly string _middlewareUnauthorizedLog =
        $"FAIL {typeof(CloudFoundrySecurityMiddleware)}: Actuator Security Error: Unauthorized - {Messages.InvalidToken}";

    private readonly string _middlewareUnavailableLog =
        $"FAIL {typeof(CloudFoundrySecurityMiddleware)}: Actuator Security Error: ServiceUnavailable - {Messages.CloudFoundryNotReachable}";

    public CloudFoundrySecurityMiddlewareTestScenarios()
    {
        Add("exception", HttpStatusCode.ServiceUnavailable,
            "Exception of type 'System.Net.Http.HttpRequestException' with error 'NameResolutionError' was thrown", [_middlewareExceptionLog], true);

        Add("exception", HttpStatusCode.OK, "Exception of type 'System.Net.Http.HttpRequestException' with error 'NameResolutionError' was thrown",
            [_middlewareExceptionLog], false);

        Add("forbidden", HttpStatusCode.Forbidden, Messages.AccessDenied, [
            _permissionsCheckForbiddenLog,
            _middlewareForbiddenLog
        ], true);

        Add("forbidden", HttpStatusCode.Forbidden, Messages.AccessDenied, [
            _permissionsCheckForbiddenLog,
            _middlewareForbiddenLog
        ], false);

        Add("no_sensitive_data", HttpStatusCode.OK, null, [_middlewareForbiddenLog], true);

        Add("not-found", HttpStatusCode.Unauthorized, Messages.InvalidToken, [
            _permissionsCheckNotFoundLog,
            _middlewareUnauthorizedLog
        ], true);

        Add("not-found", HttpStatusCode.Unauthorized, Messages.InvalidToken, [
            _permissionsCheckNotFoundLog,
            _middlewareUnauthorizedLog
        ], false);

        Add("success", HttpStatusCode.OK, null, [SuccessLog], true);

        Add("timeout", HttpStatusCode.ServiceUnavailable, Messages.CloudFoundryTimeout, [_middlewareTimeoutLog], true);

        Add("timeout", HttpStatusCode.OK, Messages.CloudFoundryTimeout, [_middlewareTimeoutLog], false);

        Add("unauthorized", HttpStatusCode.Unauthorized, Messages.InvalidToken, [
            _permissionsCheckUnauthorizedLog,
            _middlewareUnauthorizedLog
        ], true);

        Add("unauthorized", HttpStatusCode.Unauthorized, Messages.InvalidToken, [
            _permissionsCheckUnauthorizedLog,
            _middlewareUnauthorizedLog
        ], false);

        Add("unavailable", HttpStatusCode.ServiceUnavailable, Messages.CloudFoundryNotReachable, [_middlewareUnavailableLog], true);

        Add("unavailable", HttpStatusCode.OK, Messages.CloudFoundryNotReachable, [_middlewareUnavailableLog], false);
    }
}
