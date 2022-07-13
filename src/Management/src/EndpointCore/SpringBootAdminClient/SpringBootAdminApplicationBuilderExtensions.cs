// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Http;
using Steeltoe.Management.Endpoint.Health;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient;

[Obsolete("This extension will be removed in a future release, please use SpringBootAdminClientHostedService instead")]
public static class SpringBootAdminApplicationBuilderExtensions
{
    private const int ConnectionTimeoutMs = 100000;

    internal static RegistrationResult RegistrationResult { get; set; }

    /// <summary>
    /// Register the application with a Spring-Boot-Admin server.
    /// </summary>
    /// <param name="builder"><see cref="IApplicationBuilder"/>.</param>
    /// <param name="configuration">App configuration. Will be retrieved from builder.ApplicationServices if not provided.</param>
    /// <param name="httpClient">A customized HttpClient. [Bring your own auth].</param>
    public static void RegisterWithSpringBootAdmin(this IApplicationBuilder builder, IConfiguration configuration = null, HttpClient httpClient = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        configuration ??= builder.ApplicationServices.GetRequiredService<IConfiguration>();

        var logger = builder.ApplicationServices.GetService<ILogger<SpringBootAdminClientOptions>>();
        var appInfo = builder.ApplicationServices.GetApplicationInstanceInfo();
        var options = new SpringBootAdminClientOptions(configuration, appInfo);
        var managementOptions = new ManagementEndpointOptions(configuration);
        var healthOptions = new HealthEndpointOptions(configuration);
        var basePath = options.BasePath.TrimEnd('/');
        httpClient ??= HttpClientHelper.GetHttpClient(options.ValidateCertificates, ConnectionTimeoutMs);

        var app = new Application
        {
            Name = options.ApplicationName ?? "Steeltoe",
            HealthUrl = new Uri($"{basePath}{managementOptions.Path}/{healthOptions.Path}"),
            ManagementUrl = new Uri($"{basePath}{managementOptions.Path}"),
            ServiceUrl = new Uri($"{basePath}/"),
            Metadata = new Dictionary<string, object> { { "startup", DateTime.Now } },
        };

        app.Metadata.Merge(options.Metadata);

        var lifetime = builder.ApplicationServices.GetService<IHostApplicationLifetime>();
        lifetime.ApplicationStarted.Register(() =>
        {
            logger?.LogInformation("Registering with Spring Boot Admin Server at {0}", options.Url);

            var result = httpClient.PostAsJsonAsync($"{options.Url}/instances", app).GetAwaiter().GetResult();
            if (result.IsSuccessStatusCode)
            {
                RegistrationResult = result.Content.ReadFromJsonAsync<RegistrationResult>().GetAwaiter().GetResult();
            }
            else
            {
                logger.LogError($"Error registering with SpringBootAdmin {result}");
            }
        });

        lifetime.ApplicationStopped.Register(() =>
        {
            if (RegistrationResult == null || string.IsNullOrEmpty(RegistrationResult.Id))
            {
                return;
            }

            _ = httpClient.DeleteAsync($"{options.Url}/instances/{RegistrationResult.Id}").GetAwaiter().GetResult();
        });
    }

    private static void Merge<TKey, TValue>(this IDictionary<TKey, TValue> to, IDictionary<TKey, TValue> from) =>
        from?.ToList().ForEach(to.Add);
}
