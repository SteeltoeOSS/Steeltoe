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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using System.Linq;

namespace Steeltoe.Extensions.Logging.DynamicLogger
{
    public static class DynamicLoggerHostBuilderExtensions
    {
        public static IHostBuilder AddDynamicLogging(this IHostBuilder hostBuilder)
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
                });
        }
    }
}
