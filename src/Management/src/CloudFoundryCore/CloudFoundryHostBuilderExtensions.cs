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
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Hypermedia;
using System.Linq;

namespace Steeltoe.Management.CloudFoundry
{
    public static class CloudFoundryHostBuilderExtensions
    {
        /// <summary>
        /// Adds all Actuators supported by Apps Manager. Also configures DynamicLogging if not previously setup.
        /// </summary>
        /// <param name="webHostBuilder">Your Hostbuilder</param>
        public static IWebHostBuilder AddCloudFoundryActuators(this IWebHostBuilder webHostBuilder)
        {
            return webHostBuilder.AddCloudFoundryActuators(MediaTypeVersion.V1, ActuatorContext.CloudFoundry);
        }

        /// <summary>
        /// Adds all Actuators supported by Apps Manager. Also configures DynamicLogging if not previously setup.
        /// </summary>
        /// <param name="hostBuilder">Your Hostbuilder</param>
        public static IHostBuilder AddCloudFoundryActuators(this IHostBuilder hostBuilder)
        {
            return hostBuilder.AddCloudFoundryActuators(MediaTypeVersion.V1, ActuatorContext.CloudFoundry);
        }

        /// <summary>
        /// Adds all Actuators supported by Apps Manager. Also configures DynamicLogging if not previously setup.
        /// </summary>
        /// <param name="webHostBuilder">Your Hostbuilder</param>
        /// <param name="mediaTypeVersion">Spring Boot media type version to use with responses</param>
        /// <param name="actuatorContext">Select how targeted to Apps Manager actuators should be</param>
        public static IWebHostBuilder AddCloudFoundryActuators(this IWebHostBuilder webHostBuilder, MediaTypeVersion mediaTypeVersion, ActuatorContext actuatorContext)
        {
            return webHostBuilder
                .ConfigureLogging(ilb =>
                {
                    // remove the original ConsoleLoggerProvider to prevent duplicate logging
                    var serviceDescriptor = ilb.Services.FirstOrDefault(descriptor => descriptor.ImplementationType == typeof(ConsoleLoggerProvider));
                    if (serviceDescriptor != null)
                    {
                        ilb.Services.Remove(serviceDescriptor);
                    }

                    // make sure logger provider configurations are available
                    if (!ilb.Services.Any(descriptor => descriptor.ServiceType == typeof(ILoggerProviderConfiguration<ConsoleLoggerProvider>)))
                    {
                        ilb.AddConfiguration();
                    }

                    ilb.AddDynamicConsole();
                })
                .ConfigureServices((context, collection) =>
                {
                    collection.AddCloudFoundryActuators(context.Configuration, mediaTypeVersion, actuatorContext);
                    collection.AddSingleton<IStartupFilter>(new CloudFoundryActuatorsStartupFilter(mediaTypeVersion, actuatorContext));
                });
        }

        /// <summary>
        /// Adds all Actuators supported by Apps Manager. Also configures DynamicLogging if not previously setup.
        /// </summary>
        /// <param name="hostBuilder">Your Hostbuilder</param>
        /// <param name="mediaTypeVersion">Spring Boot media type version to use with responses</param>
        /// <param name="actuatorContext">Select how targeted to Apps Manager actuators should be</param>
        public static IHostBuilder AddCloudFoundryActuators(this IHostBuilder hostBuilder, MediaTypeVersion mediaTypeVersion, ActuatorContext actuatorContext)
        {
            return hostBuilder
                .ConfigureLogging(ilb =>
                {
                    // remove the original ConsoleLoggerProvider to prevent duplicate logging
                    var serviceDescriptor = ilb.Services.FirstOrDefault(descriptor => descriptor.ImplementationType == typeof(ConsoleLoggerProvider));
                    if (serviceDescriptor != null)
                    {
                        ilb.Services.Remove(serviceDescriptor);
                    }

                    // make sure logger provider configurations are available
                    if (!ilb.Services.Any(descriptor => descriptor.ServiceType == typeof(ILoggerProviderConfiguration<ConsoleLoggerProvider>)))
                    {
                        ilb.AddConfiguration();
                    }

                    ilb.AddDynamicConsole();
                })
                .ConfigureServices((context, collection) =>
                {
                    collection.AddCloudFoundryActuators(context.Configuration, mediaTypeVersion, actuatorContext);
                    collection.AddSingleton<IStartupFilter>(new CloudFoundryActuatorsStartupFilter(mediaTypeVersion, actuatorContext));
                });
        }
    }
}
