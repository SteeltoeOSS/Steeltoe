// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Http;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

public class SecurityBase
{
    public const int DefaultGetPermissionsTimeout = 5000; // Milliseconds
    public const string ApplicationIdMissingMessage = "Application id is not available";
    public const string EndpointNotConfiguredMessage = "Endpoint is not available";
    public const string AuthorizationHeaderInvalid = "Authorization header is missing or invalid";
    public const string CloudfoundryApiMissingMessage = "Cloud controller URL is not available";
    public const string CloudfoundryNotReachableMessage = "Cloud controller not reachable";
    public const string AccessDeniedMessage = "Access denied";
    public const string AuthorizationHeader = "Authorization";
    public const string Bearer = "bearer";
    public const string ReadSensitiveData = "read_sensitive_data";
    private readonly ICloudFoundryOptions _options;
    private readonly IManagementOptions _managementOptions;
    private readonly ILogger _logger;
    private HttpClient _httpClient;

    public SecurityBase(ICloudFoundryOptions options, IManagementOptions managementOptions, ILogger logger = null, HttpClient httpClient = null)
    {
        _options = options;
        _managementOptions = managementOptions;
        _logger = logger;
        _httpClient = httpClient;
    }

    public bool IsCloudFoundryRequest(string requestPath)
    {
        string contextPath = _managementOptions == null ? _options.Path : _managementOptions.Path;
        return requestPath.StartsWith(contextPath, StringComparison.InvariantCultureIgnoreCase);
    }

    public string Serialize(SecurityResult error)
    {
        try
        {
            return JsonSerializer.Serialize(error);
        }
        catch (Exception e)
        {
            _logger?.LogError("Serialization Exception: {0}", e);
        }

        return string.Empty;
    }

    public async Task<SecurityResult> GetPermissionsAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return new SecurityResult(HttpStatusCode.Unauthorized, AuthorizationHeaderInvalid);
        }

        string checkPermissionsUri = $"{_options.CloudFoundryApi}/v2/apps/{_options.ApplicationId}/permissions";
        var request = new HttpRequestMessage(HttpMethod.Get, checkPermissionsUri);
        var auth = new AuthenticationHeaderValue("bearer", token);
        request.Headers.Authorization = auth;

        // If certificate validation is disabled, inject a callback to handle properly
        HttpClientHelper.ConfigureCertificateValidation(_options.ValidateCertificates, out SecurityProtocolType prevProtocols,
            out RemoteCertificateValidationCallback prevValidator);

        try
        {
            _logger?.LogDebug("GetPermissions({0}, {1})", checkPermissionsUri, SecurityUtilities.SanitizeInput(token));
            _httpClient ??= HttpClientHelper.GetHttpClient(_options.ValidateCertificates, DefaultGetPermissionsTimeout);
            using HttpResponseMessage response = await _httpClient.SendAsync(request).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger?.LogInformation("Cloud Foundry returned status: {HttpStatus} while obtaining permissions from: {PermissionsUri}", response.StatusCode,
                    checkPermissionsUri);

                return response.StatusCode == HttpStatusCode.Forbidden
                    ? new SecurityResult(HttpStatusCode.Forbidden, AccessDeniedMessage)
                    : new SecurityResult(HttpStatusCode.ServiceUnavailable, CloudfoundryNotReachableMessage);
            }

            return new SecurityResult(await GetPermissions(response).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            _logger?.LogError("Cloud Foundry returned exception: {SecurityException} while obtaining permissions from: {PermissionsUri}", e,
                checkPermissionsUri);

            return new SecurityResult(HttpStatusCode.ServiceUnavailable, CloudfoundryNotReachableMessage);
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_options.ValidateCertificates, prevProtocols, prevValidator);
        }
    }

    public async Task<Permissions> GetPermissions(HttpResponseMessage response)
    {
        string json = string.Empty;
        var permissions = Permissions.None;

        try
        {
            json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            _logger?.LogDebug("GetPermissions returned json: {0}", SecurityUtilities.SanitizeInput(json));

            var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (result.TryGetValue(ReadSensitiveData, out JsonElement perm))
            {
                bool boolResult = JsonSerializer.Deserialize<bool>(perm.GetRawText());
                permissions = boolResult ? Permissions.Full : Permissions.Restricted;
            }
        }
        catch (Exception e)
        {
            _logger?.LogError("Exception {0} extracting permissions from {1}", e, SecurityUtilities.SanitizeInput(json));
            throw;
        }

        _logger?.LogDebug("GetPermissions returning: {0}", permissions);
        return permissions;
    }
}
