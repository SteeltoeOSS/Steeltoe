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
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

internal sealed class PermissionsProvider
{
    private const string AuthorizationHeaderInvalid = "Authorization header is missing or invalid";
    private const string CloudfoundryNotReachableMessage = "Cloud controller not reachable";
    private const string ReadSensitiveData = "read_sensitive_data";
    public const string HttpClientName = "CloudFoundrySecurity";
    public const string ApplicationIdMissingMessage = "Application id is not available";
    public const string EndpointNotConfiguredMessage = "Endpoint is not available";
    public const string CloudfoundryApiMissingMessage = "Cloud controller URL is not available";
    public const string AccessDeniedMessage = "Access denied";
    public const string AuthorizationHeader = "Authorization";
    public const string Bearer = "bearer";
    private static readonly TimeSpan GetPermissionsTimeout = TimeSpan.FromMilliseconds(5000);

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
        string checkPermissionsUri = $"{options.CloudFoundryApi}/v2/apps/{options.ApplicationId}/permissions";
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(checkPermissionsUri, UriKind.RelativeOrAbsolute));
        var auth = new AuthenticationHeaderValue("bearer", accessToken);
        request.Headers.Authorization = auth;

        try
        {
            _logger.LogDebug("GetPermissionsAsync({Uri}, {AccessToken})", checkPermissionsUri, SecurityUtilities.SanitizeInput(accessToken));
            using HttpClient httpClient = CreateHttpClient();
            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

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
        catch (Exception exception) when (!exception.IsCancellation())
        {
            _logger.LogError(exception, "Cloud Foundry returned exception while obtaining permissions from: {PermissionsUri}", checkPermissionsUri);

            return new SecurityResult(HttpStatusCode.ServiceUnavailable, CloudfoundryNotReachableMessage);
        }
    }

    public async Task<Permissions> GetPermissionsAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);

        string json = string.Empty;
        var permissions = Permissions.None;

        try
        {
            json = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("GetPermissionsAsync returned json: {Json}", SecurityUtilities.SanitizeInput(json));

            var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (result != null && result.TryGetValue(ReadSensitiveData, out JsonElement permissionElement))
            {
                bool enabled = JsonSerializer.Deserialize<bool>(permissionElement.GetRawText());
                permissions = enabled ? Permissions.Full : Permissions.Restricted;
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
