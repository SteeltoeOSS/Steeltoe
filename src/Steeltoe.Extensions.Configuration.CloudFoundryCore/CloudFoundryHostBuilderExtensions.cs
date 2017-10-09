//
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
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace Steeltoe.Extensions.Configuration
{
    public static class CloudFoundryHostBuilderExtensions
    {
        public static IWebHostBuilder UseCloudFoundryHosting(this IWebHostBuilder webHostBuilder)
        {
            if (webHostBuilder == null)
            {
                throw new ArgumentNullException(nameof(webHostBuilder));
            }

            List<string> urls = new List<string>();

            string portStr = Environment.GetEnvironmentVariable("PORT");
            if (!string.IsNullOrWhiteSpace(portStr))
            {
                int port;
                if (int.TryParse(portStr, out port))
                {
                    urls.Add($"http://*:{port}");
                }
            }

            if (urls.Count > 0)
            {
                webHostBuilder.UseUrls(urls.ToArray());
            }

            return webHostBuilder;
        }
        public static IWebHostBuilder AddCloudFoundryConfiguration(this IWebHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddCloudFoundry();
            });
            return hostBuilder;
        }
    }

}

