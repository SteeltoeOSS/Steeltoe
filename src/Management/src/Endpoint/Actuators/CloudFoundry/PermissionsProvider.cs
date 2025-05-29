// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Headers;
using System.Security;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Extensions;
using Steeltoe.Common.Http;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.CloudFoundry;

internal sealed class PermissionsProvider
{
    private const string ReadSensitiveDataJsonPropertyName = "read_sensitive_data";
    public const string AccessDeniedMessage = "Access denied";
    public const string ApplicationIdMissingMessage = "Application ID is not available";
    public const string AuthorizationHeaderName = "Authorization";
    public const string AuthorizationHeaderInvalid = "Authorization header is missing or invalid";
    public const string CloudfoundryApiMissingMessage = "Cloud controller URL is not available";
    public const string CloudfoundryNotReachableMessage = "Cloud controller not reachable";
    public const string HttpClientName = "CloudFoundrySecurity";
    public const string InvalidTokenMessage = "Invalid token";
    private static readonly TimeSpan GetPermissionsTimeout = TimeSpan.FromMilliseconds(5_000);

    private readonly IOptionsMonitor<CloudFoundryEndpointOptions> _optionsMonitor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PermissionsProvider> _logger;

    public PermissionsProvider(IOptionsMonitor<CloudFoundryEndpointOptions> optionsMonitor, IHttpClientFactory httpClientFactory,
        ILogger<PermissionsProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _optionsMonitor = optionsMonitor;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public static bool IsCloudFoundryRequest(PathString requestPath)
    {
        return requestPath.StartsWithSegments(ConfigureManagementOptions.DefaultCloudFoundryPath);
    }

    public async Task<SecurityResult> GetPermissionsAsync(string accessToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            return new SecurityResult(HttpStatusCode.Unauthorized, AuthorizationHeaderInvalid);
        }

        CloudFoundryEndpointOptions options = _optionsMonitor.CurrentValue;
        string checkPermissionsUri = $"{options.Api}/v2/apps/{options.ApplicationId}/permissions";
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(checkPermissionsUri, UriKind.RelativeOrAbsolute));
        var auth = new AuthenticationHeaderValue("bearer", accessToken);
        request.Headers.Authorization = auth;

        try
        {
            _logger.LogDebug("GetPermissionsAsync({Uri})", checkPermissionsUri);
            using HttpClient httpClient = CreateHttpClient();
            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogInformation("Cloud Foundry returned status: {HttpStatus} while obtaining permissions from: {PermissionsUri}", response.StatusCode,
                    checkPermissionsUri);

                if (response.StatusCode is HttpStatusCode.Forbidden)
                {
                    return new SecurityResult(HttpStatusCode.Forbidden, AccessDeniedMessage);
                }

                return (int)response.StatusCode is > 399 and < 500
                    ? new SecurityResult(HttpStatusCode.Unauthorized, InvalidTokenMessage)
                    : new SecurityResult(HttpStatusCode.ServiceUnavailable, CloudfoundryNotReachableMessage);
            }

            EndpointPermissions permissions = await ParsePermissionsAsync(response, cancellationToken);
            return new SecurityResult(permissions);
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            _logger.LogError(exception, "Cloud Foundry returned exception while obtaining permissions from: {PermissionsUri}", checkPermissionsUri);

            return new SecurityResult(HttpStatusCode.InternalServerError, exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogInformation(exception, "Task cancelled or timed out while obtaining permissions from: {PermissionsUri}", checkPermissionsUri);

            return new SecurityResult(HttpStatusCode.ServiceUnavailable, CloudfoundryNotReachableMessage);
        }
    }

    public async Task<EndpointPermissions> ParsePermissionsAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);

        string json = string.Empty;
        var permissions = EndpointPermissions.None;

        try
        {
            json = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("GetPermissionsAsync returned json: {Json}", SecurityUtilities.SanitizeInput(json));

            var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (result != null && result.TryGetValue(ReadSensitiveDataJsonPropertyName, out JsonElement permissionElement))
            {
                bool enabled = JsonSerializer.Deserialize<bool>(permissionElement.GetRawText());
                permissions = enabled ? EndpointPermissions.Full : EndpointPermissions.Restricted;
            }
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            throw new SecurityException($"Exception extracting permissions from json: {SecurityUtilities.SanitizeInput(json)}", exception);
        }

        _logger.LogDebug("GetPermissionsAsync returning: {Permissions}", permissions);
        return permissions;
    }

    private HttpClient CreateHttpClient()
    {
        HttpClient httpClient = _httpClientFactory.CreateClient(HttpClientName);
        httpClient.ConfigureForSteeltoe(GetPermissionsTimeout);
        return httpClient;
    }
}
