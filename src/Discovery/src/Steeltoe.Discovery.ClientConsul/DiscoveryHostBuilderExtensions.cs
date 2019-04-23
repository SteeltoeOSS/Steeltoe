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

using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Discovery.Consul.Discovery;

namespace Steeltoe.Discovery.ClientConsul
{
    public static class DiscoveryHostBuilderExtensions
    {
        public static IWebHostBuilder UseConsulDiscoveryClient(this IWebHostBuilder builder)
        {
            return builder
                .ConfigureServices((context, services) =>
                {
                    services.PostConfigure<ConsulDiscoveryOptions>(options =>
                    {
                        if (options.Port.HasValue)
                        {
                            return;
                        }

                        var urls = context.Configuration["urls"]
                            ?.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
                        var availablePort = ResolveAvailablePort(urls);

                        if (availablePort.HasValue)
                        {
                            options.Port = availablePort;
                        }
                    });

                    services.AddConsulDiscoveryClient(context.Configuration);
                    services.AddTransient<IStartupFilter, StartupFilter>();
                });
        }

        private static int? ResolveAvailablePort(string[] urls)
        {
            if (urls == null || !urls.Any())
            {
                return null;
            }

            var item = urls
                .Select(ResolveHostAndPort)
                .FirstOrDefault(i => IsAvailablePort(i.Host, i.Port));

            return item.Port;
        }

        private static bool IsAvailablePort(string host, int? port)
        {
            if (string.IsNullOrWhiteSpace(host) || !port.HasValue)
            {
                return false;
            }

            if (host == "+" || host == "*")
            {
                return true;
            }

            var ipAddresses = Dns.GetHostAddresses(host);

            return !ipAddresses.All(IPAddress.IsLoopback);
        }

        private static (string Host, int? Port) ResolveHostAndPort(string url)
        {
            string temp;
            if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                temp = url.Substring(7);
            }
            else if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                temp = url.Substring(8);
            }
            else
            {
                return (null, null);
            }

            var portStartIndex = temp.IndexOf(':');

            if (portStartIndex == -1 || portStartIndex + 1 == temp.Length)
            {
                return (null, null);
            }

            var host = temp.Substring(0, portStartIndex);
            var portString = temp.Substring(portStartIndex + 1);

            return (host, int.TryParse(portString, out var port) ? port as int? : null);
        }

        internal class StartupFilter : IStartupFilter
        {
            #region Implementation of IStartupFilter

            /// <inheritdoc/>
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return builder =>
                {
                    builder.UseConsulDiscoveryClient();
                    next(builder);
                };
            }

            #endregion Implementation of IStartupFilter
        }
    }
}