// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Http;

namespace Steeltoe.Security.DataProtection.CredHub;

public class CredHubClient : ICredHubClient
{
    private const int DefaultTimeout = 3000;

    private static HttpClient _httpClient;
    private static ILogger _logger;
    private static string _baseCredHubUrl;

    private readonly bool _validateCertificates;

    internal JsonSerializerOptions SerializerOptions { get; set; } = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public CredHubClient(bool validateCertificates = true)
    {
        _validateCertificates = validateCertificates;
    }

    /// <summary>
    /// Initialize a CredHub Client with user credentials for the appropriate UAA server.
    /// </summary>
    /// <param name="credHubOptions">
    /// CredHub client configuration values.
    /// </param>
    /// <param name="logger">
    /// Pass in a logger if you want logs.
    /// </param>
    /// <param name="httpClient">
    /// Primarily for tests, optionally provide your own http client.
    /// </param>
    /// <returns>
    /// An initialized CredHub client (using UAA OAuth).
    /// </returns>
    public static Task<CredHubClient> CreateUaaClientAsync(CredHubOptions credHubOptions, ILogger logger = null, HttpClient httpClient = null)
    {
        _logger = logger;
        _baseCredHubUrl = credHubOptions.CredHubUrl;
        var client = new CredHubClient(credHubOptions.ValidateCertificates);
        var httpClientHandler = new HttpClientHandler();
        _httpClient = httpClient ?? client.InitializeHttpClient(httpClientHandler);
        return client.InitializeAsync(credHubOptions);
    }

    private HttpClient InitializeHttpClient(HttpClientHandler httpClientHandler)
    {
        return HttpClientHelper.GetHttpClient(_validateCertificates, httpClientHandler, DefaultTimeout);
    }

    private async Task<CredHubClient> InitializeAsync(CredHubOptions options)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out SecurityProtocolType protocolType,
            out RemoteCertificateValidationCallback prevValidator);

        try
        {
            Uri tokenUri;
            string uaaOverrideUrl = Environment.GetEnvironmentVariable("UAA_Server_Override");

            if (string.IsNullOrEmpty(uaaOverrideUrl))
            {
                HttpResponseMessage info = await _httpClient.GetAsync($"{_baseCredHubUrl.Replace("/api", "/info")}").ConfigureAwait(false);

                CredHubServerInfo infoResponse =
                    await HandleErrorParseResponseAsync<CredHubServerInfo>(info, "GET /info from CredHub Server").ConfigureAwait(false);

                tokenUri = new Uri($"{infoResponse.AuthServer.First().Value}/oauth/token");
                _logger?.LogInformation($"Targeted CredHub server uses UAA server at {tokenUri}");
            }
            else
            {
                tokenUri = new Uri(uaaOverrideUrl);
                _logger?.LogInformation($"UAA set by ENV variable {tokenUri}");
            }

            // login to UAA
            string token = await HttpClientHelper.GetAccessTokenAsync(tokenUri, options.ClientId, options.ClientSecret,
                additionalParams: new Dictionary<string, string>
                {
                    { "response_type", "token" }
                }, httpClient: _httpClient, logger: _logger);

            if (token != null)
            {
                // set the token
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                return this;
            }
            else
            {
                throw new AuthenticationException("Authentication with UAA Server failed");
            }
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public async Task<CredHubCredential<T>> WriteAsync<T>(CredentialSetRequest credentialRequest)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out SecurityProtocolType protocolType,
            out RemoteCertificateValidationCallback prevValidator);

        try
        {
            _logger?.LogTrace($"About to PUT {_baseCredHubUrl}/v1/data");

            HttpResponseMessage response =
                await _httpClient.PutAsJsonAsync($"{_baseCredHubUrl}/v1/data", credentialRequest, SerializerOptions).ConfigureAwait(false);

            return await HandleErrorParseResponseAsync<CredHubCredential<T>>(response, $"Write  {typeof(T).Name}").ConfigureAwait(false);
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public async Task<CredHubCredential<T>> GenerateAsync<T>(CredHubGenerateRequest requestParameters)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out SecurityProtocolType protocolType,
            out RemoteCertificateValidationCallback prevValidator);

        try
        {
            _logger?.LogTrace($"About to POST {_baseCredHubUrl}/v1/data");

            HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync($"{_baseCredHubUrl}/v1/data", requestParameters, SerializerOptions).ConfigureAwait(false);

            return await HandleErrorParseResponseAsync<CredHubCredential<T>>(response, $"Generate {typeof(T).Name}").ConfigureAwait(false);
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public Task<CredHubCredential<T>> RegenerateAsync<T>(string name)
    {
        ArgumentGuard.NotNullOrEmpty(name);

        return RegenerateInternalAsync<T>(name);
    }

    private async Task<CredHubCredential<T>> RegenerateInternalAsync<T>(string name)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out SecurityProtocolType protocolType,
            out RemoteCertificateValidationCallback prevValidator);

        try
        {
            _logger?.LogTrace($"About to POST {_baseCredHubUrl}/v1/data");

            HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"{_baseCredHubUrl}/v1/regenerate", new Dictionary<string, string>
            {
                { "name", name }
            }).ConfigureAwait(false);

            return await HandleErrorParseResponseAsync<CredHubCredential<T>>(response, $"Regenerate  {typeof(T).Name}").ConfigureAwait(false);
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public Task<RegeneratedCertificates> BulkRegenerateAsync(string certificateAuthority)
    {
        ArgumentGuard.NotNullOrEmpty(certificateAuthority);

        return BulkRegenerateInternalAsync(certificateAuthority);
    }

    private async Task<RegeneratedCertificates> BulkRegenerateInternalAsync(string certificateAuthority)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out SecurityProtocolType protocolType,
            out RemoteCertificateValidationCallback prevValidator);

        try
        {
            _logger?.LogTrace($"About to POST {_baseCredHubUrl}/v1/bulk-regenerate");

            HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"{_baseCredHubUrl}/v1/bulk-regenerate", new Dictionary<string, string>
            {
                { "signed_by", certificateAuthority }
            }).ConfigureAwait(false);

            return await HandleErrorParseResponseAsync<RegeneratedCertificates>(response, "Bulk Regenerate Credentials").ConfigureAwait(false);
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public Task<CredHubCredential<T>> GetByIdAsync<T>(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Id of credential is required.", nameof(id));
        }

        return GetByIdInternalAsync<T>(id);
    }

    private async Task<CredHubCredential<T>> GetByIdInternalAsync<T>(Guid id)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out SecurityProtocolType protocolType,
            out RemoteCertificateValidationCallback prevValidator);

        try
        {
            _logger?.LogTrace($"About to GET {_baseCredHubUrl}v1/data/{id}");
            HttpResponseMessage response = await _httpClient.GetAsync($"{_baseCredHubUrl}v1/data/{id}").ConfigureAwait(false);
            return await HandleErrorParseResponseAsync<CredHubCredential<T>>(response, $"Get {typeof(T).Name} by Id").ConfigureAwait(false);
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public Task<CredHubCredential<T>> GetByNameAsync<T>(string name)
    {
        ArgumentGuard.NotNullOrEmpty(name);

        return GetByNameInternalAsync<T>(name);
    }

    private async Task<CredHubCredential<T>> GetByNameInternalAsync<T>(string name)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out SecurityProtocolType protocolType,
            out RemoteCertificateValidationCallback prevValidator);

        try
        {
            _logger?.LogTrace($"About to GET {_baseCredHubUrl}/v1/data?name={name}&current=true");
            HttpResponseMessage response = await _httpClient.GetAsync($"{_baseCredHubUrl}/v1/data?name={name}&current=true").ConfigureAwait(false);
            return (await HandleErrorParseResponseAsync<CredHubResponse<T>>(response, $"Get {typeof(T).Name} by Name").ConfigureAwait(false)).Data.First();
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public Task<List<CredHubCredential<T>>> GetByNameWithHistoryAsync<T>(string name, int entries = 10)
    {
        ArgumentGuard.NotNullOrEmpty(name);

        return GetByNameWithHistoryInternalAsync<T>(name, entries);
    }

    private async Task<List<CredHubCredential<T>>> GetByNameWithHistoryInternalAsync<T>(string name, int entries)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out SecurityProtocolType protocolType,
            out RemoteCertificateValidationCallback prevValidator);

        try
        {
            _logger?.LogTrace($"About to GET {_baseCredHubUrl}/v1/data?name={name}&versions={entries}");
            HttpResponseMessage response = await _httpClient.GetAsync($"{_baseCredHubUrl}/v1/data?name={name}&versions={entries}").ConfigureAwait(false);
            return (await HandleErrorParseResponseAsync<CredHubResponse<T>>(response, "Get credential by name with History").ConfigureAwait(false)).Data;
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public Task<List<FoundCredential>> FindByNameAsync(string name)
    {
        ArgumentGuard.NotNullOrEmpty(name);

        return FindByNameInternalAsync(name);
    }

    private async Task<List<FoundCredential>> FindByNameInternalAsync(string name)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out SecurityProtocolType protocolType,
            out RemoteCertificateValidationCallback prevValidator);

        try
        {
            _logger?.LogTrace($"About to GET {_baseCredHubUrl}/v1/data?name-like={name}");
            HttpResponseMessage response = await _httpClient.GetAsync($"{_baseCredHubUrl}/v1/data?name-like={name}").ConfigureAwait(false);
            return (await HandleErrorParseResponseAsync<CredentialFindResponse>(response, "Find credential by Name").ConfigureAwait(false)).Credentials;
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public Task<List<FoundCredential>> FindByPathAsync(string path)
    {
        ArgumentGuard.NotNullOrEmpty(path);

        return FindByPathInternalAsync(path);
    }

    private async Task<List<FoundCredential>> FindByPathInternalAsync(string path)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out SecurityProtocolType protocolType,
            out RemoteCertificateValidationCallback prevValidator);

        try
        {
            _logger?.LogTrace($"About to GET {_baseCredHubUrl}/v1/data?path={path}");
            HttpResponseMessage response = await _httpClient.GetAsync($"{_baseCredHubUrl}/v1/data?path={path}").ConfigureAwait(false);
            return (await HandleErrorParseResponseAsync<CredentialFindResponse>(response, "Find by Path").ConfigureAwait(false)).Credentials;
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public Task<bool> DeleteByNameAsync(string name)
    {
        ArgumentGuard.NotNullOrEmpty(name);

        return DeleteByNameInternalAsync(name);
    }

    public async Task<bool> DeleteByNameInternalAsync(string name)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out SecurityProtocolType protocolType,
            out RemoteCertificateValidationCallback prevValidator);

        try
        {
            _logger?.LogTrace($"About to DELETE {_baseCredHubUrl}/v1/data?name={name}");
            HttpResponseMessage response = await _httpClient.DeleteAsync($"{_baseCredHubUrl}/v1/data?name={name}").ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.NotFound)
            {
                return true;
            }

            return false;
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public Task<List<CredentialPermission>> GetPermissionsAsync(string credentialName)
    {
        ArgumentGuard.NotNullOrEmpty(credentialName);

        return GetPermissionsInternalAsync(credentialName);
    }

    private async Task<List<CredentialPermission>> GetPermissionsInternalAsync(string name)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out SecurityProtocolType protocolType,
            out RemoteCertificateValidationCallback prevValidator);

        try
        {
            _logger?.LogTrace($"About to GET {_baseCredHubUrl}/v1/permissions?credential_name={name}");
            HttpResponseMessage response = await _httpClient.GetAsync($"{_baseCredHubUrl}/v1/permissions?credential_name={name}").ConfigureAwait(false);
            return (await HandleErrorParseResponseAsync<CredentialPermissions>(response, "Get Permissions").ConfigureAwait(false)).Permissions;
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public Task<List<CredentialPermission>> AddPermissionsAsync(string credentialName, List<CredentialPermission> permissions)
    {
        ArgumentGuard.NotNullOrEmpty(credentialName);
        ArgumentGuard.NotNullOrEmpty(permissions);

        return AddPermissionsInternalAsync(credentialName, permissions);
    }

    private async Task<List<CredentialPermission>> AddPermissionsInternalAsync(string name, List<CredentialPermission> permissions)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out SecurityProtocolType protocolType,
            out RemoteCertificateValidationCallback prevValidator);

        try
        {
            _logger?.LogTrace($"About to POST {_baseCredHubUrl}/v1/permissions");

            var newPermissions = new CredentialPermissions
            {
                CredentialName = name,
                Permissions = permissions
            };

            _ = await _httpClient.PostAsJsonAsync($"{_baseCredHubUrl}/v1/permissions", newPermissions, SerializerOptions).ConfigureAwait(false);

            return await GetPermissionsAsync(name).ConfigureAwait(false);
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public Task<bool> DeletePermissionAsync(string credentialName, string actor)
    {
        ArgumentGuard.NotNullOrEmpty(credentialName);
        ArgumentGuard.NotNullOrEmpty(actor);

        return DeletePermissionInternalAsync(credentialName, actor);
    }

    private async Task<bool> DeletePermissionInternalAsync(string name, string actor)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out SecurityProtocolType protocolType,
            out RemoteCertificateValidationCallback prevValidator);

        try
        {
            _logger?.LogTrace($"About to DELETE {_baseCredHubUrl}/v1/permissions?credential_name={name}&actor={actor}");

            HttpResponseMessage response = await _httpClient.DeleteAsync($"{_baseCredHubUrl}/v1/permissions?credential_name={name}&actor={actor}")
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.NotFound)
            {
                return true;
            }

            return false;
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public Task<string> InterpolateServiceDataAsync(string serviceData)
    {
        ArgumentGuard.NotNullOrEmpty(serviceData);

        return InterpolateServiceDataInternalAsync(serviceData);
    }

    private async Task<string> InterpolateServiceDataInternalAsync(string serviceData)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out SecurityProtocolType protocolType,
            out RemoteCertificateValidationCallback prevValidator);

        try
        {
            _logger?.LogTrace($"About to POST {_baseCredHubUrl}/v1/interpolate");

            HttpResponseMessage response = await _httpClient
                .PostAsync($"{_baseCredHubUrl}/v1/interpolate", new StringContent(serviceData, Encoding.Default, "application/json")).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            else
            {
                throw new CredHubException($"Failed to interpolate credentials, status code: {response.StatusCode}");
            }
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    private async Task<T> HandleErrorParseResponseAsync<T>(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<T>(SerializerOptions).ConfigureAwait(false);
        }

        _logger?.LogCritical($"Failed to {operation}, status code: {response.StatusCode}");
        _logger?.LogCritical(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        throw new CredHubException($"Failed to {operation}, status code: {response.StatusCode}");
    }
}
