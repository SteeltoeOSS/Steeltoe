// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Steeltoe.Common.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Security.DataProtection.CredHub
{
    public class CredHubClient : ICredHubClient
    {
        private const int DEFAULT_TIMEOUT = 3000;

        private static HttpClient _httpClient;
        private static HttpClientHandler _httpClientHandler;
        private static ILogger _logger;
        private static string _baseCredHubUrl;
        private JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

        private bool _validateCertificates;

        public CredHubClient(bool validateCertificates = true)
        {
            _validateCertificates = validateCertificates;
        }

        /// <summary>
        /// Expects CF_INSTANCE_CERT and CF_INSTANCE_KEY to be set in the environment (automatically set by DIEGO in cloud foundry)
        /// </summary>
        /// <param name="credHubOptions">CredHub client configuration values</param>
        /// <param name="logger">Pass in a logger if you want logs</param>
        /// <param name="httpClient">Optionally override the http client used to talk to credhub - added for tests only</param>
        /// <returns>An initialized CredHub client (using mTLS)</returns>
        public static async Task<CredHubClient> CreateMTLSClientAsync(CredHubOptions credHubOptions, ILogger logger = null, HttpClient httpClient = null)
        {
            _logger = logger;
            _baseCredHubUrl = credHubOptions.CredHubUrl;

            var cfInstanceCert = Environment.GetEnvironmentVariable("CF_INSTANCE_CERT") ?? string.Empty;
            var cfInstanceKey = Environment.GetEnvironmentVariable("CF_INSTANCE_KEY");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && cfInstanceCert.StartsWith("/"))
            {
                _logger?.LogTrace("Detected Windows OS and root-relative paths for application credentials: converting to app-relative paths");
                cfInstanceCert = ".." + cfInstanceCert;
                cfInstanceKey = ".." + cfInstanceKey;
            }

            if (string.IsNullOrEmpty(cfInstanceCert) || string.IsNullOrEmpty(cfInstanceKey))
            {
                _logger?.LogCritical("Cloud Foundry application credentials not found in the environment");
                throw new ArgumentException("Application Credentials not found (Missing ENV variable for Instance Cert and/or Key)");
            }

            _logger?.LogTrace("Application certificate: " + cfInstanceCert);
            _logger?.LogTrace("Application key: " + cfInstanceKey);
            if (File.Exists(cfInstanceCert) && File.Exists(cfInstanceKey))
            {
                var client = new CredHubClient(credHubOptions.ValidateCertificates);
                _httpClientHandler = new HttpClientHandler()
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual
                };
                var certBytes = File.ReadAllBytes(cfInstanceCert);
                var keyBytes = File.ReadAllBytes(cfInstanceKey);
                var appCredentials = CertificateHelpers.GetX509FromBytes(certBytes, keyBytes);
                if (!appCredentials.HasPrivateKey)
                {
                    throw new Exception("Private key is missing, mTLS won't work");
                }

                _httpClientHandler.ClientCertificates.Add(appCredentials);
                _httpClient = httpClient ?? client.InitializeHttpClient(_httpClientHandler);

                return await client.InitializeAsync();
            }
            else
            {
                throw new Exception($"Application credentials not found (Failed to load Instance Cert [{cfInstanceCert}] and/or Key [{cfInstanceKey}])");
            }
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
            return client.InitializeAsync(credHubOptions.CredHubUser, credHubOptions.CredHubPassword);
        }

        private HttpClient InitializeHttpClient(HttpClientHandler httpClientHandler)
        {
            return HttpClientHelper.GetHttpClient(_validateCertificates, httpClientHandler, DEFAULT_TIMEOUT);
        }

        private async Task<CredHubClient> InitializeAsync()
        {
            HttpClientHelper.ConfigureCertificateValidatation(_validateCertificates, out SecurityProtocolType protocolType, out RemoteCertificateValidationCallback prevValidator);
            try
            {
                var info = await _httpClient.GetAsync($"{_baseCredHubUrl.Replace("/api", "/info")}");
                if (!info.IsSuccessStatusCode)
                {
                    // we don't NEED to throw an error here as this was more of a connectivity test than anything else
                    // throw new CredHubException($"Failed calling {_baseCredHubUrl.Replace("/api", "/info")}: {info.StatusCode}");
                    _logger?.LogError($"Failed calling {_baseCredHubUrl.Replace("/api", "/info")}: {info.StatusCode} -- CredHub interactions may not work!");
                }
            }
            catch (Exception e)
            {
                _logger?.LogCritical($"Encountered an exception calling /info on the CredHub server: {e}");
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
            }

            return this;
        }

        private async Task<CredHubClient> InitializeAsync(string credHubUser, string credHubPassword)
        {
            HttpClientHelper.ConfigureCertificateValidatation(_validateCertificates, out SecurityProtocolType protocolType, out RemoteCertificateValidationCallback prevValidator);
            try
            {
                Uri tokenUri;
                var uaaOverrideUrl = Environment.GetEnvironmentVariable("UAA_Server_Override");
                if (string.IsNullOrEmpty(uaaOverrideUrl))
                {
                    var info = await _httpClient.GetAsync($"{_baseCredHubUrl.Replace("/api", "/info")}");
                    var infoResponse = await HandleErrorParseResponse<CredHubServerInfo>(info, "GET /info from CredHub Server");
                    tokenUri = new Uri($"{infoResponse.AuthServer.First().Value}/oauth/token");
                    _logger?.LogInformation($"Targeted CredHub server uses UAA server at {tokenUri}");
                }
                else
                {
                    tokenUri = new Uri(uaaOverrideUrl);
                    _logger?.LogInformation($"UAA set by ENV variable {tokenUri}");
                }

                // login to UAA
                var header = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{credHubUser}:{credHubPassword}")));
                _httpClient.DefaultRequestHeaders.Authorization = header;
                var postParams = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("response_type", "token")
            };
                var response = await _httpClient.PostAsync(tokenUri, new FormUrlEncodedContent(postParams));

                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogTrace(await response.Content.ReadAsStringAsync());

                    // set the token
                    var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.Value<string>("access_token"));

                    return this;
                }
                else
                {
                    _logger?.LogCritical($"Authentication with UAA Server failed, status code: {response.StatusCode}");
                    _logger?.LogCritical(await response.Content.ReadAsStringAsync());
                    throw new AuthenticationException($"Authentication with UAA Server failed, status code: {response.StatusCode}");
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
            HttpClientHelper.ConfigureCertificateValidatation(_validateCertificates, out SecurityProtocolType protocolType, out RemoteCertificateValidationCallback prevValidator);
            try
            {
                _logger?.LogTrace($"About to PUT {_baseCredHubUrl}/v1/data");
                var response = await _httpClient.PutAsJsonAsync($"{_baseCredHubUrl}/v1/data", credentialRequest, _serializerSettings);

                var dataAsString = await response.Content.ReadAsStringAsync();
                var s = JsonConvert.DeserializeObject<CredHubCredential<T>>(dataAsString, _serializerSettings);

                return await HandleErrorParseResponse<CredHubCredential<T>>(response, $"Write  {typeof(T).Name}");
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
            }
        }
#pragma warning restore SA1202 // Elements must be ordered by access

        public async Task<CredHubCredential<T>> GenerateAsync<T>(CredHubGenerateRequest request)
        {
            HttpClientHelper.ConfigureCertificateValidatation(_validateCertificates, out SecurityProtocolType protocolType, out RemoteCertificateValidationCallback prevValidator);
            try
            {
                _logger?.LogTrace($"About to POST {_baseCredHubUrl}/v1/data");
                var response = await _httpClient.PostAsJsonAsync($"{_baseCredHubUrl}/v1/data", request, _serializerSettings);
                return await HandleErrorParseResponse<CredHubCredential<T>>(response, $"Generate {typeof(T).Name}");
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
            }
        }

        public async Task<CredHubCredential<T>> RegenerateAsync<T>(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name of credential to regenerate is required");
            }

            HttpClientHelper.ConfigureCertificateValidatation(_validateCertificates, out SecurityProtocolType protocolType, out RemoteCertificateValidationCallback prevValidator);
            try
            {
                _logger?.LogTrace($"About to POST {_baseCredHubUrl}/v1/data");
                var response = await _httpClient.PostAsJsonAsync($"{_baseCredHubUrl}/v1/regenerate", new Dictionary<string, string> { { "name", name } });
                return await HandleErrorParseResponse<CredHubCredential<T>>(response, $"Regenerate  {typeof(T).Name}");
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
            }
        }

        public async Task<RegeneratedCertificates> BulkRegenerateAsync(string certificateAuthority)
        {
            if (string.IsNullOrEmpty(certificateAuthority))
            {
                throw new ArgumentException("Certificate authority used for certificates is required");
            }

            HttpClientHelper.ConfigureCertificateValidatation(_validateCertificates, out SecurityProtocolType protocolType, out RemoteCertificateValidationCallback prevValidator);
            try
            {
                _logger?.LogTrace($"About to POST {_baseCredHubUrl}/v1/bulk-regenerate");
                var response = await _httpClient.PostAsJsonAsync($"{_baseCredHubUrl}/v1/bulk-regenerate", new Dictionary<string, string> { { "signed_by", certificateAuthority } });
                return await HandleErrorParseResponse<RegeneratedCertificates>(response, "Bulk Regenerate Credentials");
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
            }
        }

        public async Task<CredHubCredential<T>> GetByIdAsync<T>(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Id of credential is required");
            }

            HttpClientHelper.ConfigureCertificateValidatation(_validateCertificates, out SecurityProtocolType protocolType, out RemoteCertificateValidationCallback prevValidator);
            try
            {
                _logger?.LogTrace($"About to GET {_baseCredHubUrl}/v1/data{id}");
                var response = await _httpClient.GetAsync($"{_baseCredHubUrl}/v1/data/{id}");
                return await HandleErrorParseResponse<CredHubCredential<T>>(response, $"Get {typeof(T).Name} by Id");
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
            }
        }

        public async Task<CredHubCredential<T>> GetByNameAsync<T>(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name of credential is required");
            }

            HttpClientHelper.ConfigureCertificateValidatation(_validateCertificates, out SecurityProtocolType protocolType, out RemoteCertificateValidationCallback prevValidator);
            try
            {
                _logger?.LogTrace($"About to GET {_baseCredHubUrl}/v1/data?name={name}&current=true");
                var response = await _httpClient.GetAsync($"{_baseCredHubUrl}/v1/data?name={name}&current=true");
                return (await HandleErrorParseResponse<CredHubResponse<T>>(response, $"Get {typeof(T).Name} by Name")).Data.First();
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
            }
        }

        public async Task<List<CredHubCredential<T>>> GetByNameWithHistoryAsync<T>(string name, int entries = 10)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name is required");
            }

            HttpClientHelper.ConfigureCertificateValidatation(_validateCertificates, out SecurityProtocolType protocolType, out RemoteCertificateValidationCallback prevValidator);
            try
            {
                _logger?.LogTrace($"About to GET {_baseCredHubUrl}/v1/data?name={name}&versions={entries}");
                var response = await _httpClient.GetAsync($"{_baseCredHubUrl}/v1/data?name={name}&versions={entries}");
                return (await HandleErrorParseResponse<CredHubResponse<T>>(response, "Get credential by name with History")).Data;
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
            }
        }

        public async Task<List<FoundCredential>> FindByNameAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name is required");
            }

            HttpClientHelper.ConfigureCertificateValidatation(_validateCertificates, out SecurityProtocolType protocolType, out RemoteCertificateValidationCallback prevValidator);
            try
            {
                _logger?.LogTrace($"About to GET {_baseCredHubUrl}/v1/data?name-like={name}");
                var response = await _httpClient.GetAsync($"{_baseCredHubUrl}/v1/data?name-like={name}");
                return (await HandleErrorParseResponse<CredentialFindResponse>(response, "Find credential by Name")).Credentials;
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
            }
        }

        public async Task<List<FoundCredential>> FindByPathAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path is required");
            }

            HttpClientHelper.ConfigureCertificateValidatation(_validateCertificates, out SecurityProtocolType protocolType, out RemoteCertificateValidationCallback prevValidator);
            try
            {
                _logger?.LogTrace($"About to GET {_baseCredHubUrl}/v1/data?path={path}");
                var response = await _httpClient.GetAsync($"{_baseCredHubUrl}/v1/data?path={path}");
                return (await HandleErrorParseResponse<CredentialFindResponse>(response, "Find by Path")).Credentials;
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
            }
        }

        public async Task<List<CredentialPath>> FindAllPathsAsync()
        {
            HttpClientHelper.ConfigureCertificateValidatation(_validateCertificates, out SecurityProtocolType protocolType, out RemoteCertificateValidationCallback prevValidator);
            try
            {
                _logger?.LogTrace($"About to GET {_baseCredHubUrl}/v1/data?paths=true");
                var response = await _httpClient.GetAsync($"{_baseCredHubUrl}/v1/data?paths=true");
                return (await HandleErrorParseResponse<CredentialPathsResponse>(response, "Find all Paths")).Paths;
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
            }
        }

        public async Task<bool> DeleteByNameAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name of credential to regenerate is required");
            }

            HttpClientHelper.ConfigureCertificateValidatation(_validateCertificates, out SecurityProtocolType protocolType, out RemoteCertificateValidationCallback prevValidator);
            try
            {
                _logger?.LogTrace($"About to DELETE {_baseCredHubUrl}/v1/data?name={name}");
                var response = await _httpClient.DeleteAsync($"{_baseCredHubUrl}/v1/data?name={name}");

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

        public async Task<List<CredentialPermission>> GetPermissionsAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name is required");
            }

            HttpClientHelper.ConfigureCertificateValidatation(_validateCertificates, out SecurityProtocolType protocolType, out RemoteCertificateValidationCallback prevValidator);
            try
            {
                _logger?.LogTrace($"About to GET {_baseCredHubUrl}/v1/permissions?credential_name={name}");
                var response = await _httpClient.GetAsync($"{_baseCredHubUrl}/v1/permissions?credential_name={name}");
                return (await HandleErrorParseResponse<CredentialPermissions>(response, "Get Permissions")).Permissions;
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
            }
        }

        public async Task<List<CredentialPermission>> AddPermissionsAsync(string name, List<CredentialPermission> permissions)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name is required");
            }

            if (permissions == null || !permissions.Any())
            {
                throw new ArgumentException("At least one permission is required");
            }

            HttpClientHelper.ConfigureCertificateValidatation(_validateCertificates, out SecurityProtocolType protocolType, out RemoteCertificateValidationCallback prevValidator);
            try
            {
                _logger?.LogTrace($"About to POST {_baseCredHubUrl}/v1/permissions");
                var newPermissions = new CredentialPermissions { CredentialName = name, Permissions = permissions };
                var addResponse = await _httpClient.PostAsJsonAsync($"{_baseCredHubUrl}/v1/permissions", newPermissions, _serializerSettings);

                return await GetPermissionsAsync(name);
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(_validateCertificates, protocolType, prevValidator);
            }
        }

        public async Task<bool> DeletePermissionAsync(string name, string actor)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name is required");
            }

            if (string.IsNullOrEmpty(actor))
            {
                throw new ArgumentException("Actor is required");
            }

            HttpClientHelper.ConfigureCertificateValidatation(_validateCertificates, out SecurityProtocolType protocolType, out RemoteCertificateValidationCallback prevValidator);
            try
            {
                _logger?.LogTrace($"About to DELETE {_baseCredHubUrl}/v1/permissions?credential_name={name}&actor={actor}");
                var response = await _httpClient.DeleteAsync($"{_baseCredHubUrl}/v1/permissions?credential_name={name}&actor={actor}");

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

        public async Task<string> InterpolateServiceDataAsync(string serviceData)
        {
            if (string.IsNullOrEmpty(serviceData))
            {
                throw new ArgumentException("Service data is required");
            }

            HttpClientHelper.ConfigureCertificateValidatation(_validateCertificates, out SecurityProtocolType protocolType, out RemoteCertificateValidationCallback prevValidator);
            try
            {
                _logger?.LogTrace($"About to POST {_baseCredHubUrl}/v1/interpolate");
                var response = await _httpClient.PostAsync($"{_baseCredHubUrl}/v1/interpolate", new StringContent(serviceData, Encoding.Default, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
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
                return await response.Content.ReadAsJsonAsync<T>();
            }
            else
            {
                _logger?.LogCritical($"Failed to {operation}, status code: {response.StatusCode}");
                _logger?.LogCritical(await response.Content.ReadAsStringAsync());
                throw new CredHubException($"Failed to {operation}, status code: {response.StatusCode}");
            }
        }
    }
}
