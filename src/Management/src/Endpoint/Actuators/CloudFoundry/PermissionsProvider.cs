// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
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

            EndpointPermissions? permissions = await ParsePermissionsResponseAsync(response, cancellationToken);

            return permissions != null
                ? new SecurityResult(permissions.Value)
                : new SecurityResult(HttpStatusCode.BadGateway, Messages.CloudFoundryBrokenResponse);
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

    public async Task<EndpointPermissions?> ParsePermissionsResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);

        try
        {
            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            LogResponseJson(SecurityUtilities.SanitizeInput(json));

            var result = JsonSerializer.Deserialize<PermissionsResponse>(json);

            EndpointPermissions permissions = result switch
            {
                { ReadBasicData: true, ReadSensitiveData: true } => EndpointPermissions.Full,
                { ReadBasicData: true, ReadSensitiveData: false } => EndpointPermissions.Restricted,
                _ => EndpointPermissions.None
            };

            LogPermissions(permissions);
            return permissions;
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            return null;
        }
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

    private sealed class PermissionsResponse
    {
        [JsonPropertyName("read_basic_data")]
        public bool ReadBasicData { get; set; }

        [JsonPropertyName("read_sensitive_data")]
        public bool ReadSensitiveData { get; set; }
    }

    internal static class Messages
    {
        public const string AccessDenied = "Access denied";
        public const string ApplicationIdMissing = "Application ID is not available";
        public const string AuthorizationHeaderInvalid = "Authorization header is missing or invalid";
        public const string CloudFoundryApiMissing = "Cloud controller URL is not available";
        public const string CloudFoundryNotReachable = "Cloud controller not reachable";
        public const string CloudFoundryTimeout = "Cloud controller request timed out";
        public const string CloudFoundryBrokenResponse = "Failed to parse Cloud controller response";
        public const string InvalidToken = "Invalid token";
    }
}
