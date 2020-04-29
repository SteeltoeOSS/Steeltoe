// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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

            var appInfo = builder.ApplicationServices.GetApplicationInstanceInfo();
            var options = new BootAdminClientOptions(configuration, appInfo);
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
            var lifetime = builder.ApplicationServices.GetService<IHostApplicationLifetime>();
            lifetime.ApplicationStarted.Register(() =>
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
            });

            lifetime.ApplicationStopped.Register(() =>
            {
                if (RegistrationResult == null || string.IsNullOrEmpty(RegistrationResult.Id))
                {
                    return;
                }

                var httpClient = testClient ?? new HttpClient();
                var result = httpClient.DeleteAsync($"{options.Url}/instances/{RegistrationResult.Id}").Result;
            });
        }
    }
}
