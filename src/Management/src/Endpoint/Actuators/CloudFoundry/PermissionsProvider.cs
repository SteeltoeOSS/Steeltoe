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

internal sealed partial class PermissionsProvider
{
    private const string ReadSensitiveDataJsonPropertyName = "read_sensitive_data";
    public const string HttpClientName = "CloudFoundrySecurity";
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
            return new SecurityResult(HttpStatusCode.Unauthorized, Messages.AuthorizationHeaderInvalid);
        }

        CloudFoundryEndpointOptions options = _optionsMonitor.CurrentValue;
        string checkPermissionsUri = $"{options.Api}/v2/apps/{options.ApplicationId}/permissions";
        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(checkPermissionsUri, UriKind.RelativeOrAbsolute));
        var auth = new AuthenticationHeaderValue("bearer", accessToken);
        request.Headers.Authorization = auth;

        try
        {
            LogGetPermissions(checkPermissionsUri);
            using HttpClient httpClient = CreateHttpClient();
            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                LogResponseStatus(response.StatusCode, checkPermissionsUri);

                if (response.StatusCode is HttpStatusCode.Forbidden)
                {
                    return new SecurityResult(HttpStatusCode.Forbidden, Messages.AccessDenied);
                }

                return (int)response.StatusCode is > 399 and < 500
                    ? new SecurityResult(HttpStatusCode.Unauthorized, Messages.InvalidToken)
                    : new SecurityResult(HttpStatusCode.ServiceUnavailable, Messages.CloudFoundryNotReachable);
            }

            EndpointPermissions permissions = await ParsePermissionsResponseAsync(response, cancellationToken);
            return new SecurityResult(permissions);
        }
        catch (HttpRequestException exception)
        {
            return new SecurityResult(HttpStatusCode.ServiceUnavailable,
                $"Exception of type '{typeof(HttpRequestException)}' with error '{exception.HttpRequestError}' was thrown");
        }
        catch (Exception exception) when (exception.IsHttpClientTimeout())
        {
            return new SecurityResult(HttpStatusCode.ServiceUnavailable, Messages.CloudFoundryTimeout);
        }
    }

    public async Task<EndpointPermissions> ParsePermissionsResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);

        string json = string.Empty;
        var permissions = EndpointPermissions.None;

        try
        {
            json = await response.Content.ReadAsStringAsync(cancellationToken);

            LogResponseJson(SecurityUtilities.SanitizeInput(json));

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

        LogPermissions(permissions);
        return permissions;
    }

    private HttpClient CreateHttpClient()
    {
        HttpClient httpClient = _httpClientFactory.CreateClient(HttpClientName);
        httpClient.ConfigureForSteeltoe(GetPermissionsTimeout);
        return httpClient;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching permissions from {PermissionsUri}.")]
    private partial void LogGetPermissions(string permissionsUri);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cloud Foundry returned status {HttpStatus} while obtaining permissions from {PermissionsUri}.")]
    private partial void LogResponseStatus(HttpStatusCode httpStatus, string permissionsUri);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Permissions response returned JSON: {Json}")]
    private partial void LogResponseJson(string json);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Resolved permissions to {Permissions}.")]
    private partial void LogPermissions(EndpointPermissions permissions);

    internal static class Messages
    {
        public const string AccessDenied = "Access denied";
        public const string ApplicationIdMissing = "Application ID is not available";
        public const string AuthorizationHeaderInvalid = "Authorization header is missing or invalid";
        public const string CloudFoundryApiMissing = "Cloud controller URL is not available";
        public const string CloudFoundryNotReachable = "Cloud controller not reachable";
        public const string CloudFoundryTimeout = "Cloud controller request timed out";
        public const string InvalidToken = "Invalid token";
    }
}
