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

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Steeltoe.Discovery.Client
{
    public static class DiscoveryHostBuilderExtensions
    {
        /// <summary>
        /// Adds service discovery to your application based on app configuration. This method can be used in place of configuration via your Startup class.
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        public static IWebHostBuilder AddServiceDiscovery(this IWebHostBuilder hostBuilder)
        {
            return hostBuilder
                .ConfigureServices((context, collection) =>
                {
                    collection.AddDiscoveryClient(context.Configuration);
                    collection.AddTransient<IStartupFilter, DiscoveryClientStartupFilter>();
                });
        }

        /// <summary>
        /// Adds service discovery to your application based on app configuration. This method can be used in place of configuration via your Startup class.
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        public static IHostBuilder AddServiceDiscovery(this IHostBuilder hostBuilder)
        {
            return hostBuilder
                .ConfigureServices((context, collection) =>
                {
                    collection.AddDiscoveryClient(context.Configuration);
                    collection.AddTransient<IStartupFilter, DiscoveryClientStartupFilter>();
                });
        }
    }
}
