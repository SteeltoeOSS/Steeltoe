// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Extensions;
using Steeltoe.Common.Http;
using Steeltoe.Common.Util;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Transport;
using Steeltoe.Discovery.Eureka.Util;

namespace Steeltoe.Discovery.Eureka;

/// <summary>
/// Performs HTTP requests to Eureka servers.
/// </summary>
public sealed class EurekaClient
{
    // HTTP endpoints are described at: https://github.com/Netflix/eureka/wiki/Eureka-REST-operations

    private const string MediaType = "application/json";
    private const string DiscoveryAllowRedirectHeaderName = "X-Discovery-AllowRedirect";
    private static readonly Task<object?> TaskOfNull = Task.FromResult<object?>(null);
    private static readonly TimeSpan GetAccessTokenTimeout = TimeSpan.FromSeconds(10);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<EurekaClientOptions> _optionsMonitor;
    private readonly EurekaServiceUriStateManager _eurekaServiceUriStateManager;
    private readonly ILogger<EurekaClient> _logger;

    private static JsonSerializerOptions ResponseSerializerOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonApplicationConverter(),
            new JsonInstanceInfoConverter()
        }
    };

    public EurekaClient(IHttpClientFactory httpClientFactory, IOptionsMonitor<EurekaClientOptions> optionsMonitor,
        EurekaServiceUriStateManager eurekaServiceUriStateManager, ILogger<EurekaClient> logger)
    {
        ArgumentGuard.NotNull(httpClientFactory);
        ArgumentGuard.NotNull(optionsMonitor);
        ArgumentGuard.NotNull(eurekaServiceUriStateManager);
        ArgumentGuard.NotNull(logger);

        _httpClientFactory = httpClientFactory;
        _optionsMonitor = optionsMonitor;
        _eurekaServiceUriStateManager = eurekaServiceUriStateManager;
        _logger = logger;
    }

    /// <summary>
    /// Registers an application instance.
    /// </summary>
    /// <param name="instance">
    /// The instance to register.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <exception cref="EurekaTransportException">
    /// The registration failed because none of the Eureka servers responded with success.
    /// </exception>
    public async Task RegisterAsync(InstanceInfo instance, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(instance);

        if ((Platform.IsContainerized || Platform.IsCloudHosted) && string.Equals(instance.HostName, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Registering with hostname 'localhost' in containerized or cloud environments may not be valid. Please configure Eureka:Instance:HostName with a non-localhost address.");
        }

        HttpContent requestContent = new StringContent(JsonSerializer.Serialize(new JsonInstanceInfoRoot
        {
            Instance = instance.ToJsonInstance()
        }), Encoding.UTF8, MediaType);

        await ExecuteRequestAsync(HttpMethod.Post, $"apps/{instance.AppName}", null, requestContent, cancellationToken);
    }

    /// <summary>
    /// Deregisters an application instance.
    /// </summary>
    /// <param name="appId">
    /// The ID of the app to deregister.
    /// </param>
    /// <param name="instanceId">
    /// The ID of the instance to deregister.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <exception cref="EurekaTransportException">
    /// The registration failed because none of the Eureka servers responded with success.
    /// </exception>
    public async Task DeregisterAsync(string appId, string instanceId, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNullOrEmpty(appId);
        ArgumentGuard.NotNullOrEmpty(instanceId);

        await ExecuteRequestAsync(HttpMethod.Delete, $"apps/{appId}/{instanceId}", null, null, cancellationToken);
    }

    /// <summary>
    /// Sends a heartbeat for an application instance.
    /// </summary>
    /// <param name="appId">
    /// The ID of the app to send a heartbeat for.
    /// </param>
    /// <param name="instanceId">
    /// The ID of the instance to send a heartbeat for.
    /// </param>
    /// <param name="status">
    /// The new instance status.
    /// </param>
    /// <param name="lastDirtyTimeUtc">
    /// The date and time (in UTC) when the instance was last dirty.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <exception cref="EurekaTransportException">
    /// The registration failed because none of the Eureka servers responded with success.
    /// </exception>
    public async Task HeartbeatAsync(string appId, string instanceId, InstanceStatus status, DateTime lastDirtyTimeUtc, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNullOrEmpty(appId);
        ArgumentGuard.NotNullOrEmpty(instanceId);

        var queryString = new Dictionary<string, string>
        {
            ["status"] = status.ToSnakeCaseString(SnakeCaseStyle.AllCaps),
            ["lastDirtyTimestamp"] = DateTimeConversions.ToJavaMilliseconds(lastDirtyTimeUtc).ToString(CultureInfo.InvariantCulture)
        };

        await ExecuteRequestAsync(HttpMethod.Put, $"apps/{appId}/{instanceId}", queryString, null, cancellationToken);
    }

    /// <summary>
    /// Queries for all application instances.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <exception cref="EurekaTransportException">
    /// The registration failed because none of the Eureka servers responded with success.
    /// </exception>
    public Task<Applications> GetApplicationsAsync(CancellationToken cancellationToken)
    {
        return GetApplicationsAtPathAsync("apps", cancellationToken);
    }

    /// <summary>
    /// Queries for a delta of all application instances.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <exception cref="EurekaTransportException">
    /// The registration failed because none of the Eureka servers responded with success.
    /// </exception>
    public Task<Applications> GetDeltaAsync(CancellationToken cancellationToken)
    {
        return GetApplicationsAtPathAsync("apps/delta", cancellationToken);
    }

    /// <summary>
    /// Queries for all application instances under a particular VIP address.
    /// </summary>
    /// <param name="vipAddress">
    /// The Virtual Internet Protocol address whose instances to return.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <exception cref="EurekaTransportException">
    /// The registration failed because none of the Eureka servers responded with success.
    /// </exception>
    public Task<Applications> GetVipAsync(string vipAddress, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNullOrEmpty(vipAddress);

        return GetApplicationsAtPathAsync($"vips/{vipAddress}", cancellationToken);
    }

    private async Task<Applications> GetApplicationsAtPathAsync(string path, CancellationToken cancellationToken)
    {
        return await ExecuteRequestAsync(HttpMethod.Get, path, null, null, async response =>
        {
            var root = await response.Content.ReadFromJsonAsync<JsonApplicationsRoot>(ResponseSerializerOptions, cancellationToken);
            return Applications.FromJsonApplications(root!.Applications);
        }, cancellationToken);
    }

    private async Task ExecuteRequestAsync(HttpMethod method, string path, IDictionary<string, string>? queryString, HttpContent? requestContent,
        CancellationToken cancellationToken)
    {
        _ = await ExecuteRequestAsync(method, path, queryString, requestContent, _ => TaskOfNull, cancellationToken);
    }

    private async Task<TResult> ExecuteRequestAsync<TResult>(HttpMethod method, string path, IDictionary<string, string>? queryString,
        HttpContent? requestContent, Func<HttpResponseMessage, Task<TResult>> getResultAsync, CancellationToken cancellationToken)
    {
        using HttpClient httpClient = CreateHttpClient("Eureka");
        EurekaServiceUriStateManager.ServiceUrisSnapshot serviceUris = _eurekaServiceUriStateManager.GetSnapshot();

        int tryCount = _optionsMonitor.CurrentValue.EurekaServer.RetryCount + 1;

        for (int attempt = 1; attempt <= tryCount; attempt++)
        {
            Uri serviceUri = serviceUris.GetNextServiceUri();
            Uri requestUri = GetRequestUri(serviceUri, path, queryString);

            HttpRequestMessage request = await GetRequestMessageAsync(method, requestUri, requestContent, cancellationToken);

            try
            {
                using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

                _logger.LogDebug("HTTP request to '{RequestUri}' returned status {StatusCode} in attempt {Attempt}.", requestUri.ToMaskedUri(),
                    (int)response.StatusCode, attempt);

                if (response.IsSuccessStatusCode)
                {
                    _eurekaServiceUriStateManager.MarkWorkingServiceUri(serviceUri);

                    try
                    {
                        return await getResultAsync(response);
                    }
                    catch (JsonException exception) when (!exception.IsCancellation())
                    {
                        _logger.LogDebug(exception, "Failed to deserialize HTTP response from '{RequestUri}'.", requestUri.ToMaskedUri());
                    }
                }
                else
                {
                    string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                    _logger.LogInformation("HTTP request to '{RequestUri}' failed with status {StatusCode}: {ResponseBody}", requestUri.ToMaskedUri(),
                        (int)response.StatusCode, responseBody);
                }
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger.LogWarning(exception, "Failed to execute HTTP request to '{RequestUri}' in attempt {Attempt}.", requestUri.ToMaskedUri(), attempt);
            }

            _eurekaServiceUriStateManager.MarkFailingServiceUri(serviceUri);
        }

        throw new EurekaTransportException("Retry limit reached; giving up on completing the HTTP request.");
    }

    private HttpClient CreateHttpClient(string name)
    {
        HttpClient httpClient = _httpClientFactory.CreateClient(name);

        int connectTimeoutSeconds = _optionsMonitor.CurrentValue.EurekaServer.ConnectTimeoutSeconds;

        if (connectTimeoutSeconds > 0)
        {
            httpClient.Timeout = TimeSpan.FromSeconds(connectTimeoutSeconds);
        }

        return httpClient;
    }

    private static Uri GetRequestUri(Uri baseUri, string path, IDictionary<string, string>? queryString = null)
    {
        var requestUri = new Uri(baseUri, path);

        if (queryString != null)
        {
            var uriBuilder = new UriBuilder(requestUri);
            var queryBuilder = new QueryBuilder(queryString);
            uriBuilder.Query = queryBuilder.ToQueryString().ToUriComponent();
            requestUri = uriBuilder.Uri;
        }

        return requestUri;
    }

    private async Task<HttpRequestMessage> GetRequestMessageAsync(HttpMethod method, Uri requestUri, HttpContent? content, CancellationToken cancellationToken)
    {
        var uriWithoutUserInfo = new Uri(requestUri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.SafeUnescaped));
        var requestMessage = new HttpRequestMessage(method, uriWithoutUserInfo);

        if (requestUri.TryGetUsernamePassword(out string? username, out string? password) && password.Length > 0)
        {
            requestMessage.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}")));
        }
        else
        {
            EurekaClientOptions options = _optionsMonitor.CurrentValue;

            if (!string.IsNullOrEmpty(options.AccessTokenUri))
            {
                using HttpClient httpClient = CreateHttpClient("AccessTokenForEureka");

                string accessToken = await httpClient.GetAccessTokenAsync(new Uri(options.AccessTokenUri), options.ClientId, options.ClientSecret,
                    GetAccessTokenTimeout, cancellationToken);

                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        requestMessage.Headers.Add("Accept", MediaType);
        requestMessage.Headers.Add(DiscoveryAllowRedirectHeaderName, "false");

        requestMessage.Content = content;

        return requestMessage;
    }
}
