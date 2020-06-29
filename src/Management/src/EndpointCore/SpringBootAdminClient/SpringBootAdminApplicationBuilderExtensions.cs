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
using System.Net.Http;
using System.Net.Http.Json;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient
{
    public static class SpringBootAdminApplicationBuilderExtensions
    {
        private const int ConnectionTimeoutMs = 100000;

        internal static RegistrationResult RegistrationResult { get; set; }

        /// <summary>
        /// Register the application with a Spring-Boot-Admin server
        /// </summary>
        /// <param name="builder"><see cref="IApplicationBuilder"/></param>
        /// <param name="configuration">App configuration. Will be retrieved from builder.ApplicationServices if not provided</param>
        /// <param name="httpClient">A customized HttpClient. [Bring your own auth]</param>
        public static void RegisterWithSpringBootAdmin(this IApplicationBuilder builder, IConfiguration configuration = null, HttpClient httpClient = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configuration is null)
            {
                configuration = builder.ApplicationServices.GetRequiredService<IConfiguration>();
            }

            var logger = builder.ApplicationServices.GetService<ILogger<SpringBootAdminClientOptions>>();
            var appInfo = builder.ApplicationServices.GetApplicationInstanceInfo();
            var options = new SpringBootAdminClientOptions(configuration, appInfo);
            var mgmtOptions = new ManagementEndpointOptions(configuration);
            var healthOptions = new HealthEndpointOptions(configuration);
            var basePath = options.BasePath.TrimEnd('/');

            var app = new Application()
            {
                Name = options.ApplicationName ?? "Steeltoe",
                HealthUrl = new Uri($"{basePath}/{mgmtOptions.Path}/{healthOptions.Path}"),
                ManagementUrl = new Uri($"{basePath}/{mgmtOptions.Path}"),
                ServiceUrl = new Uri($"{basePath}/"),
                Metadata = new Metadata() { Startup = DateTime.Now }
            };
            var lifetime = builder.ApplicationServices.GetService<IHostApplicationLifetime>();
            lifetime.ApplicationStarted.Register(() =>
            {
                logger?.LogInformation("Registering with Spring Boot Admin Server at {0}", options.Url);
                httpClient ??= HttpClientHelper.GetHttpClient(false, ConnectionTimeoutMs);

                var result = httpClient.PostAsJsonAsync($"{options.Url}/instances", app).GetAwaiter().GetResult();
                if (result.IsSuccessStatusCode)
                {
                    RegistrationResult = result.Content.ReadAsJsonAsync<RegistrationResult>().GetAwaiter().GetResult();
                }
            });

            lifetime.ApplicationStopped.Register(() =>
            {
                if (RegistrationResult == null || string.IsNullOrEmpty(RegistrationResult.Id))
                {
                    return;
                }

                httpClient ??= HttpClientHelper.GetHttpClient(false, ConnectionTimeoutMs);
                _ = httpClient.DeleteAsync($"{options.Url}/instances/{RegistrationResult.Id}").GetAwaiter().GetResult();
            });
        }
    }
}
