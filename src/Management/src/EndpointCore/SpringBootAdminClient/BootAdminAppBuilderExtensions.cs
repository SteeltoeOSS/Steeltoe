// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Steeltoe.Common;
using Steeltoe.Common.Http;
using Steeltoe.Management.Endpoint.Health;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using IApplicationLifetime = Microsoft.Extensions.Hosting.IApplicationLifetime;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient
{
    public static class BootAdminAppBuilderExtensions
    {
        private const int ConnectionTimeoutMs = 100000;
        private static RegistrationResult registrationResult;

        internal static RegistrationResult RegistrationResult { get => registrationResult; }

        public static void RegisterSpringBootAdmin(this IApplicationBuilder builder, IConfiguration configuration, HttpClient testClient = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var options = new BootAdminClientOptions(configuration);
            var mgmtOptions = new ManagementEndpointOptions(configuration);
            var healthOptions = new HealthEndpointOptions(configuration);
            var basePath = options.BasePath;

            var app = new Application()
            {
                Name = options.ApplicationName ?? "Steeltoe",
                HealthUrl = new Uri($"{basePath}{mgmtOptions.Path}/{healthOptions.Path}"),
                ManagementUrl = new Uri($"{basePath}{mgmtOptions.Path}"),
                ServiceUrl = new Uri($"{basePath}/"),
                Metadata = new Metadata() { Startup = DateTime.Now }
            };
            Action onStarted = () =>
            {
                var httpClient = testClient ?? HttpClientHelper.GetHttpClient(false, ConnectionTimeoutMs);

                var content = JsonConvert.SerializeObject(app);
                var buffer = System.Text.Encoding.UTF8.GetBytes(content);
                var byteContent = new ByteArrayContent(buffer);
                byteContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var result = httpClient.PostAsync($"{options.Url}/instances", byteContent).Result;
                if (result.IsSuccessStatusCode)
                {
                    var task = result.Content.ReadAsStringAsync();
                    registrationResult = JsonConvert.DeserializeObject<RegistrationResult>(task.Result);
                }
            };
            Action onStopped = () =>
            {
                if (RegistrationResult == null || string.IsNullOrEmpty(RegistrationResult.Id))
                {
                    return;
                }

                var httpClient = testClient ?? HttpClientHelper.GetHttpClient(false, ConnectionTimeoutMs);
                var result = httpClient.DeleteAsync($"{options.Url}/instances/{RegistrationResult.Id}").Result;
            };

#if NETCOREAPP3_0
            var lifetime = builder.ApplicationServices.GetService<IHostApplicationLifetime>();
            lifetime.ApplicationStarted.Register(onStarted);
            lifetime.ApplicationStopped.Register(onStopped);
#else
            var lifetime = builder.ApplicationServices.GetService<IApplicationLifetime>();
            lifetime.ApplicationStarted.Register(onStarted);
            lifetime.ApplicationStopped.Register(onStopped);
#endif
        }
    }
}
