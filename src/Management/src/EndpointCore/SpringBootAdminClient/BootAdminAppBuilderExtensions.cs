// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Steeltoe.Common.Http;
using Steeltoe.Management.Endpoint.Health;
using System;
using System.Net.Http;
using System.Text;
using IApplicationLifetime = Microsoft.Extensions.Hosting.IApplicationLifetime;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient
{
    public static class BootAdminAppBuilderExtensions
    {
        private const int ConnectionTimeoutMs = 100000;

        internal static RegistrationResult RegistrationResult { get; private set; }

        public static void RegisterSpringBootAdmin(this IApplicationBuilder builder, IConfiguration configuration, HttpClient httpClient = null)
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
            httpClient ??= HttpClientHelper.GetHttpClient(false, ConnectionTimeoutMs);
            void OnStarted()
            {
                var content = JsonConvert.SerializeObject(app);
                var buffer = Encoding.UTF8.GetBytes(content);
                var byteContent = new ByteArrayContent(buffer);
                byteContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var result = httpClient.PostAsync($"{options.Url}/instances", byteContent).Result;
                if (result.IsSuccessStatusCode)
                {
                    var task = result.Content.ReadAsStringAsync();
                    RegistrationResult = JsonConvert.DeserializeObject<RegistrationResult>(task.Result);
                }
            }

            void OnStopped()
            {
                if (RegistrationResult == null || string.IsNullOrEmpty(RegistrationResult.Id))
                {
                    return;
                }

                _ = httpClient.DeleteAsync($"{options.Url}/instances/{RegistrationResult.Id}").Result;
            }

#if NETCOREAPP3_1
            var lifetime = builder.ApplicationServices.GetService<IHostApplicationLifetime>();
            lifetime.ApplicationStarted.Register(OnStarted);
            lifetime.ApplicationStopped.Register(OnStopped);
#else
            var lifetime = builder.ApplicationServices.GetService<IApplicationLifetime>();
            lifetime.ApplicationStarted.Register(OnStarted);
            lifetime.ApplicationStopped.Register(OnStopped);
#endif
        }
    }
}
