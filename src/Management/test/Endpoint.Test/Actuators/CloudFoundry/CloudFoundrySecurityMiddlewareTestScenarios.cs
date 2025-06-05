// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Messages = Steeltoe.Management.Endpoint.Actuators.CloudFoundry.PermissionsProvider.Messages;

namespace Steeltoe.Management.Endpoint.Test.Actuators.CloudFoundry;

internal sealed class CloudFoundrySecurityMiddlewareTestScenarios : TheoryData<string, HttpStatusCode?, string?, string[], bool>
{
    private const string CFForbiddenLog =
        "INFO Steeltoe.Management.Endpoint.Actuators.CloudFoundry.PermissionsProvider: Cloud Foundry returned status: Forbidden while obtaining permissions from: https://example.api.com/v2/apps/forbidden/permissions";

    private const string CFExceptionLogStart = "FAIL Microsoft.AspNetCore.Server.Kestrel: Connection id";
    private const string CFExceptionLogEnd = ": An unhandled exception was thrown by the application.";

    private const string CFUnauthorizedLog =
        "INFO Steeltoe.Management.Endpoint.Actuators.CloudFoundry.PermissionsProvider: Cloud Foundry returned status: Unauthorized while obtaining permissions from: https://example.api.com/v2/apps/unauthorized/permissions";

    private const string CFNotFoundLog =
        "INFO Steeltoe.Management.Endpoint.Actuators.CloudFoundry.PermissionsProvider: Cloud Foundry returned status: NotFound while obtaining permissions from: https://example.api.com/v2/apps/not-found/permissions";

    private const string CFTimeoutLog =
        "FAIL Steeltoe.Management.Endpoint.Actuators.CloudFoundry.PermissionsProvider: Cloud Foundry request timed out while obtaining permissions from: https://example.api.com/v2/apps/timeout/permissions";

    private const string MiddlewareForbiddenLog =
        $"FAIL Steeltoe.Management.Endpoint.Actuators.CloudFoundry.CloudFoundrySecurityMiddleware: Actuator Security Error: Forbidden - {Messages.AccessDenied}";

    private const string MiddlewareTimeoutLog =
        $"FAIL Steeltoe.Management.Endpoint.Actuators.CloudFoundry.CloudFoundrySecurityMiddleware: Actuator Security Error: ServiceUnavailable - {Messages.CloudFoundryTimeout}";

    private const string MiddlewareUnauthorizedLog =
        $"FAIL Steeltoe.Management.Endpoint.Actuators.CloudFoundry.CloudFoundrySecurityMiddleware: Actuator Security Error: Unauthorized - {Messages.InvalidToken}";

    private const string MiddlewareUnavailableLog =
        $"FAIL Steeltoe.Management.Endpoint.Actuators.CloudFoundry.CloudFoundrySecurityMiddleware: Actuator Security Error: ServiceUnavailable - {Messages.CloudFoundryNotReachable}";

    private const string SuccessLog =
        "INFO System.Net.Http.HttpClient.CloudFoundrySecurity.ClientHandler: Sending HTTP request GET https://example.api.com/v2/apps/success/permissions";

    public CloudFoundrySecurityMiddlewareTestScenarios()
    {
        Add("exception", HttpStatusCode.InternalServerError, string.Empty, [
            CFExceptionLogStart,
            CFExceptionLogEnd
        ], true);

        Add("exception", HttpStatusCode.InternalServerError, string.Empty, [
            CFExceptionLogStart,
            CFExceptionLogEnd
        ], false);

        Add("forbidden", HttpStatusCode.Forbidden, Messages.AccessDenied, [
            CFForbiddenLog,
            MiddlewareForbiddenLog
        ], true);

        Add("forbidden", HttpStatusCode.Forbidden, Messages.AccessDenied, [
            CFForbiddenLog,
            MiddlewareForbiddenLog
        ], false);

        Add("no_sensitive_data", HttpStatusCode.OK, null, [MiddlewareForbiddenLog], true);

        Add("not-found", HttpStatusCode.Unauthorized, Messages.InvalidToken, [
            CFNotFoundLog,
            MiddlewareUnauthorizedLog
        ], true);

        Add("not-found", HttpStatusCode.Unauthorized, Messages.InvalidToken, [
            CFNotFoundLog,
            MiddlewareUnauthorizedLog
        ], false);

        Add("success", HttpStatusCode.OK, null, [SuccessLog], true);

        Add("timeout", HttpStatusCode.ServiceUnavailable, Messages.CloudFoundryTimeout, [
            CFTimeoutLog,
            MiddlewareTimeoutLog
        ], true);

        Add("timeout", HttpStatusCode.OK, Messages.CloudFoundryTimeout, [
            CFTimeoutLog,
            MiddlewareTimeoutLog
        ], false);

        Add("unauthorized", HttpStatusCode.Unauthorized, Messages.InvalidToken, [
            CFUnauthorizedLog,
            MiddlewareUnauthorizedLog
        ], true);

        Add("unauthorized", HttpStatusCode.Unauthorized, Messages.InvalidToken, [
            CFUnauthorizedLog,
            MiddlewareUnauthorizedLog
        ], false);

        Add("unavailable", HttpStatusCode.ServiceUnavailable, Messages.CloudFoundryNotReachable, [MiddlewareUnavailableLog], true);

        Add("unavailable", HttpStatusCode.OK, Messages.CloudFoundryNotReachable, [MiddlewareUnavailableLog], false);
    }
}
