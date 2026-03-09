// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Json;
using Steeltoe.Common.Http;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient;

internal sealed class SpringBootAdminApiClient
{
    internal const string HttpClientName = "SpringBootAdmin";

    private readonly IHttpClientFactory _httpClientFactory;

    public SpringBootAdminApiClient(IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);

        _httpClientFactory = httpClientFactory;
    }

    public async Task<string?> RegisterAsync(Application application, SpringBootAdminClientOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(options);

        using HttpClient httpClient = CreateHttpClient(options.ConnectionTimeout);

        string requestUri = $"{options.Url}/instances";
        HttpResponseMessage response = await httpClient.PostAsJsonAsync(requestUri, application, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string errorResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Error response from HTTP POST request at {requestUri}: {errorResponse}");
        }

        var registrationResult = await response.Content.ReadFromJsonAsync<RegistrationResult>(cancellationToken);
        return registrationResult?.Id;
    }

    public async Task UnregisterAsync(string id, SpringBootAdminClientOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(options);

        using HttpClient httpClient = CreateHttpClient(options.ConnectionTimeout);

        var requestUri = new Uri($"{options.Url}/instances/{id}");
        HttpResponseMessage response = await httpClient.DeleteAsync(requestUri, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string errorResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Error response from HTTP DELETE request at {requestUri}: {errorResponse}");
        }
    }

    private HttpClient CreateHttpClient(TimeSpan timeout)
    {
        HttpClient httpClient = _httpClientFactory.CreateClient(HttpClientName);
        httpClient.ConfigureForSteeltoe(timeout);
        return httpClient;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    internal sealed class RegistrationResult
    {
        public string? Id { get; set; }
    }
}
