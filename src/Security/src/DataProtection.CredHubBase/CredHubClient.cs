// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Steeltoe.Security.DataProtection.CredHub;

public class CredHubClient : ICredHubClient
{
    internal JsonSerializerOptions SerializerOptions { get; set; } = new JsonSerializerOptions
    {
        IgnoreNullValues = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private const int DEFAULT_TIMEOUT = 3000;

    private static HttpClient _httpClient;
    private static HttpClientHandler _httpClientHandler;
    private static ILogger _logger;
    private static string _baseCredHubUrl;

    private readonly bool _validateCertificates;

    public CredHubClient(bool validateCertificates = true)
    {
        _validateCertificates = validateCertificates;
    }

    /// <summary>
    /// Initialize a CredHub Client with user credentials for the appropriate UAA server
    /// </summary>
    /// <param name="credHubOptions">CredHub client configuration values</param>
    /// <param name="logger">Pass in a logger if you want logs</param>
    /// <param name="httpClient">Primarily for tests, optionally provide your own http client</param>
    /// <returns>An initialized CredHub client (using UAA OAuth)</returns>
    public static Task<CredHubClient> CreateUAAClientAsync(CredHubOptions credHubOptions, ILogger logger = null, HttpClient httpClient = null)
    {
        _logger = logger;
        _baseCredHubUrl = credHubOptions.CredHubUrl;
        var client = new CredHubClient(credHubOptions.ValidateCertificates);
        _httpClientHandler = new HttpClientHandler();
        _httpClient = httpClient ?? client.InitializeHttpClient(_httpClientHandler);
        return client.InitializeAsync(credHubOptions);
    }

    private HttpClient InitializeHttpClient(HttpClientHandler httpClientHandler)
    {
        return HttpClientHelper.GetHttpClient(_validateCertificates, httpClientHandler, DEFAULT_TIMEOUT);
    }

    private async Task<CredHubClient> InitializeAsync(CredHubOptions options)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out var protocolType, out var prevValidator);
        try
        {
            Uri tokenUri;
            var uaaOverrideUrl = Environment.GetEnvironmentVariable("UAA_Server_Override");
            if (string.IsNullOrEmpty(uaaOverrideUrl))
            {
                var info = await _httpClient.GetAsync($"{_baseCredHubUrl.Replace("/api", "/info")}").ConfigureAwait(false);
                var infoResponse = await HandleErrorParseResponse<CredHubServerInfo>(info, "GET /info from CredHub Server").ConfigureAwait(false);
                tokenUri = new Uri($"{infoResponse.AuthServer.First().Value}/oauth/token");
                _logger?.LogInformation($"Targeted CredHub server uses UAA server at {tokenUri}");
            }
            else
            {
                tokenUri = new Uri(uaaOverrideUrl);
                _logger?.LogInformation($"UAA set by ENV variable {tokenUri}");
            }

            // login to UAA
            var token = await HttpClientHelper.GetAccessToken(
                tokenUri,
                options.ClientId,
                options.ClientSecret,
                additionalParams: new Dictionary<string, string> { { "response_type", "token" } },
                httpClient: _httpClient,
                logger: _logger);

            if (token is object)
            {
                // set the token
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                return this;
            }
            else
            {
                throw new AuthenticationException($"Authentication with UAA Server failed");
            }
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

#pragma warning disable SA1202 // Elements must be ordered by access
    public async Task<CredHubCredential<T>> WriteAsync<T>(CredentialSetRequest credentialRequest)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out var protocolType, out var prevValidator);
        try
        {
            _logger?.LogTrace($"About to PUT {_baseCredHubUrl}/v1/data");
            var response = await _httpClient.PutAsJsonAsync($"{_baseCredHubUrl}/v1/data", credentialRequest, SerializerOptions).ConfigureAwait(false);
            return await HandleErrorParseResponse<CredHubCredential<T>>(response, $"Write  {typeof(T).Name}").ConfigureAwait(false);
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public async Task<CredHubCredential<T>> GenerateAsync<T>(CredHubGenerateRequest requestParameters)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out var protocolType, out var prevValidator);
        try
        {
            _logger?.LogTrace($"About to POST {_baseCredHubUrl}/v1/data");

            var response = await _httpClient.PostAsJsonAsync($"{_baseCredHubUrl}/v1/data", requestParameters, SerializerOptions).ConfigureAwait(false);
            return await HandleErrorParseResponse<CredHubCredential<T>>(response, $"Generate {typeof(T).Name}").ConfigureAwait(false);
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public Task<CredHubCredential<T>> RegenerateAsync<T>(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Name of credential to regenerate is required");
        }

        return RegenerateInternalAsync<T>(name);
    }

    private async Task<CredHubCredential<T>> RegenerateInternalAsync<T>(string name)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out var protocolType, out var prevValidator);
        try
        {
            _logger?.LogTrace($"About to POST {_baseCredHubUrl}/v1/data");
            var response = await _httpClient.PostAsJsonAsync($"{_baseCredHubUrl}/v1/regenerate", new Dictionary<string, string> { { "name", name } }).ConfigureAwait(false);
            return await HandleErrorParseResponse<CredHubCredential<T>>(response, $"Regenerate  {typeof(T).Name}").ConfigureAwait(false);
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public Task<RegeneratedCertificates> BulkRegenerateAsync(string certificateAuthority)
    {
        if (string.IsNullOrEmpty(certificateAuthority))
        {
            throw new ArgumentException("Certificate authority used for certificates is required");
        }

        return BulkRegenerateInternalAsync(certificateAuthority);
    }

    private async Task<RegeneratedCertificates> BulkRegenerateInternalAsync(string certificateAuthority)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out var protocolType, out var prevValidator);
        try
        {
            _logger?.LogTrace($"About to POST {_baseCredHubUrl}/v1/bulk-regenerate");
            var response = await _httpClient.PostAsJsonAsync($"{_baseCredHubUrl}/v1/bulk-regenerate", new Dictionary<string, string> { { "signed_by", certificateAuthority } }).ConfigureAwait(false);
            return await HandleErrorParseResponse<RegeneratedCertificates>(response, "Bulk Regenerate Credentials").ConfigureAwait(false);
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
            throw new ArgumentException("Id of credential is required");
        }

        return GetByIdInternalAsync<T>(id);
    }

    private async Task<CredHubCredential<T>> GetByIdInternalAsync<T>(Guid id)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out var protocolType, out var prevValidator);
        try
        {
            _logger?.LogTrace($"About to GET {_baseCredHubUrl}v1/data/{id}");
            var response = await _httpClient.GetAsync($"{_baseCredHubUrl}v1/data/{id}").ConfigureAwait(false);
            return await HandleErrorParseResponse<CredHubCredential<T>>(response, $"Get {typeof(T).Name} by Id").ConfigureAwait(false);
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public Task<CredHubCredential<T>> GetByNameAsync<T>(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Name of credential is required");
        }

        return GetByNameInternalAsync<T>(name);
    }

    private async Task<CredHubCredential<T>> GetByNameInternalAsync<T>(string name)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out var protocolType, out var prevValidator);
        try
        {
            _logger?.LogTrace($"About to GET {_baseCredHubUrl}/v1/data?name={name}&current=true");
            var response = await _httpClient.GetAsync($"{_baseCredHubUrl}/v1/data?name={name}&current=true").ConfigureAwait(false);
            return (await HandleErrorParseResponse<CredHubResponse<T>>(response, $"Get {typeof(T).Name} by Name").ConfigureAwait(false)).Data.First();
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public Task<List<CredHubCredential<T>>> GetByNameWithHistoryAsync<T>(string name, int entries = 10)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Name is required");
        }

        return GetByNameWithHistoryInternalAsync<T>(name, entries);
    }

    private async Task<List<CredHubCredential<T>>> GetByNameWithHistoryInternalAsync<T>(string name, int entries)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out var protocolType, out var prevValidator);
        try
        {
            _logger?.LogTrace($"About to GET {_baseCredHubUrl}/v1/data?name={name}&versions={entries}");
            var response = await _httpClient.GetAsync($"{_baseCredHubUrl}/v1/data?name={name}&versions={entries}").ConfigureAwait(false);
            return (await HandleErrorParseResponse<CredHubResponse<T>>(response, "Get credential by name with History").ConfigureAwait(false)).Data;
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public Task<List<FoundCredential>> FindByNameAsync(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Name is required");
        }

        return FindByNameInternalAsync(name);
    }

    private async Task<List<FoundCredential>> FindByNameInternalAsync(string name)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out var protocolType, out var prevValidator);
        try
        {
            _logger?.LogTrace($"About to GET {_baseCredHubUrl}/v1/data?name-like={name}");
            var response = await _httpClient.GetAsync($"{_baseCredHubUrl}/v1/data?name-like={name}").ConfigureAwait(false);
            return (await HandleErrorParseResponse<CredentialFindResponse>(response, "Find credential by Name").ConfigureAwait(false)).Credentials;
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public Task<List<FoundCredential>> FindByPathAsync(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("Path is required");
        }

        return FindByPathInternalAsync(path);
    }

    private async Task<List<FoundCredential>> FindByPathInternalAsync(string path)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out var protocolType, out var prevValidator);
        try
        {
            _logger?.LogTrace($"About to GET {_baseCredHubUrl}/v1/data?path={path}");
            var response = await _httpClient.GetAsync($"{_baseCredHubUrl}/v1/data?path={path}").ConfigureAwait(false);
            return (await HandleErrorParseResponse<CredentialFindResponse>(response, "Find by Path").ConfigureAwait(false)).Credentials;
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public Task<bool> DeleteByNameAsync(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Name of credential to regenerate is required");
        }

        return DeleteByNameInternalAsync(name);
    }

    public async Task<bool> DeleteByNameInternalAsync(string name)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out var protocolType, out var prevValidator);
        try
        {
            _logger?.LogTrace($"About to DELETE {_baseCredHubUrl}/v1/data?name={name}");
            var response = await _httpClient.DeleteAsync($"{_baseCredHubUrl}/v1/data?name={name}").ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent || response.StatusCode == System.Net.HttpStatusCode.NotFound)
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

    public Task<List<CredentialPermission>> GetPermissionsAsync(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Name is required");
        }

        return GetPermissionsInternalAsync(name);
    }

    private async Task<List<CredentialPermission>> GetPermissionsInternalAsync(string name)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out var protocolType, out var prevValidator);
        try
        {
            _logger?.LogTrace($"About to GET {_baseCredHubUrl}/v1/permissions?credential_name={name}");
            var response = await _httpClient.GetAsync($"{_baseCredHubUrl}/v1/permissions?credential_name={name}").ConfigureAwait(false);
            return (await HandleErrorParseResponse<CredentialPermissions>(response, "Get Permissions").ConfigureAwait(false)).Permissions;
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public Task<List<CredentialPermission>> AddPermissionsAsync(string name, List<CredentialPermission> permissions)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Name is required");
        }

        if (permissions == null || !permissions.Any())
        {
            throw new ArgumentException("At least one permission is required");
        }

        return AddPermissionsInternalAsync(name, permissions);
    }

    private async Task<List<CredentialPermission>> AddPermissionsInternalAsync(string name, List<CredentialPermission> permissions)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out var protocolType, out var prevValidator);
        try
        {
            _logger?.LogTrace($"About to POST {_baseCredHubUrl}/v1/permissions");
            var newPermissions = new CredentialPermissions { CredentialName = name, Permissions = permissions };
            _ = await _httpClient.PostAsJsonAsync($"{_baseCredHubUrl}/v1/permissions", newPermissions, SerializerOptions).ConfigureAwait(false);

            return await GetPermissionsAsync(name).ConfigureAwait(false);
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
        }
    }

    public Task<bool> DeletePermissionAsync(string name, string actor)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Name is required");
        }

        if (string.IsNullOrEmpty(actor))
        {
            throw new ArgumentException("Actor is required");
        }

        return DeletePermissionInternalAsync(name, actor);
    }

    private async Task<bool> DeletePermissionInternalAsync(string name, string actor)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out var protocolType, out var prevValidator);
        try
        {
            _logger?.LogTrace($"About to DELETE {_baseCredHubUrl}/v1/permissions?credential_name={name}&actor={actor}");
            var response = await _httpClient.DeleteAsync($"{_baseCredHubUrl}/v1/permissions?credential_name={name}&actor={actor}").ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent || response.StatusCode == System.Net.HttpStatusCode.NotFound)
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
        if (string.IsNullOrEmpty(serviceData))
        {
            throw new ArgumentException("Service data is required");
        }

        return InterpolateServiceDataInternalAsync(serviceData);
    }

    private async Task<string> InterpolateServiceDataInternalAsync(string serviceData)
    {
        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out var protocolType, out var prevValidator);
        try
        {
            _logger?.LogTrace($"About to POST {_baseCredHubUrl}/v1/interpolate");
            var response = await _httpClient.PostAsync($"{_baseCredHubUrl}/v1/interpolate", new StringContent(serviceData, Encoding.Default, "application/json")).ConfigureAwait(false);

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

    private async Task<T> HandleErrorParseResponse<T>(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<T>(SerializerOptions).ConfigureAwait(false);
        }
        else
        {
            _logger?.LogCritical($"Failed to {operation}, status code: {response.StatusCode}");
            _logger?.LogCritical(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            throw new CredHubException($"Failed to {operation}, status code: {response.StatusCode}");
        }
    }
}
#pragma warning restore SA1202 // Elements must be ordered by access