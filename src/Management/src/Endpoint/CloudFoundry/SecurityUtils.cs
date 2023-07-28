// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Http;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

internal sealed class SecurityUtils
{
    private const int DefaultGetPermissionsTimeout = 5000; // Milliseconds
    private const string AuthorizationHeaderInvalid = "Authorization header is missing or invalid";
    private const string CloudfoundryNotReachableMessage = "Cloud controller not reachable";
    private const string ReadSensitiveData = "read_sensitive_data";
    public const string ApplicationIdMissingMessage = "Application id is not available";
    public const string EndpointNotConfiguredMessage = "Endpoint is not available";
    public const string CloudfoundryApiMissingMessage = "Cloud controller URL is not available";
    public const string AccessDeniedMessage = "Access denied";
    public const string AuthorizationHeader = "Authorization";
    public const string Bearer = "bearer";
    private readonly CloudFoundryEndpointOptions _options;

    private readonly ILogger _logger;
    private HttpClient _httpClient;

    public SecurityUtils(CloudFoundryEndpointOptions options, ILogger logger, HttpClient httpClient = null)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(logger);

        _options = options;
        _logger = logger;
        _httpClient = httpClient;
    }

    public static bool IsCloudFoundryRequest(PathString requestPath)
    {
        ArgumentGuard.NotNull(requestPath);

        return requestPath.StartsWithSegments(ConfigureManagementOptions.DefaultCloudFoundryPath);
    }

    public async Task<SecurityResult> GetPermissionsAsync(string accessToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            return new SecurityResult(HttpStatusCode.Unauthorized, AuthorizationHeaderInvalid);
        }

        string checkPermissionsUri = $"{_options.CloudFoundryApi}/v2/apps/{_options.ApplicationId}/permissions";
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(checkPermissionsUri, UriKind.RelativeOrAbsolute));
        var auth = new AuthenticationHeaderValue("bearer", accessToken);
        request.Headers.Authorization = auth;

        try
        {
            _logger.LogDebug("GetPermissionsAsync({uri}, {accessToken})", checkPermissionsUri, SecurityUtilities.SanitizeInput(accessToken));
            _httpClient ??= HttpClientHelper.GetHttpClient(_options.ValidateCertificates, DefaultGetPermissionsTimeout);
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogInformation("Cloud Foundry returned status: {HttpStatus} while obtaining permissions from: {PermissionsUri}", response.StatusCode,
                    checkPermissionsUri);

                return response.StatusCode == HttpStatusCode.Forbidden
                    ? new SecurityResult(HttpStatusCode.Forbidden, AccessDeniedMessage)
                    : new SecurityResult(HttpStatusCode.ServiceUnavailable, CloudfoundryNotReachableMessage);
            }

            Permissions permissions = await GetPermissionsAsync(response, cancellationToken);
            return new SecurityResult(permissions);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Cloud Foundry returned exception while obtaining permissions from: {PermissionsUri}", checkPermissionsUri);

            return new SecurityResult(HttpStatusCode.ServiceUnavailable, CloudfoundryNotReachableMessage);
        }
    }

    public async Task<Permissions> GetPermissionsAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(response);

        string json = string.Empty;
        var permissions = Permissions.None;

        try
        {
            json = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("GetPermissionsAsync returned json: {json}", SecurityUtilities.SanitizeInput(json));

            var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (result.TryGetValue(ReadSensitiveData, out JsonElement perm))
            {
                bool boolResult = JsonSerializer.Deserialize<bool>(perm.GetRawText());
                permissions = boolResult ? Permissions.Full : Permissions.Restricted;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Exception extracting permissions from {json}", SecurityUtilities.SanitizeInput(json));
            throw;
        }

        _logger.LogDebug("GetPermissionsAsync returning: {permissions}", permissions);
        return permissions;
    }
}
