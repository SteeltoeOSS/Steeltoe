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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Steeltoe.Management.TaskCore.Test
{
    public class TestServerStartup
    {
        public TestServerStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTask("test", _ => throw new PassException());
        }

        public void Configure(IApplicationBuilder app)
        {
        }
    }

#pragma warning disable CA1032 // Implement standard exception constructors
#pragma warning disable SA1402 // File may only contain a single type
    internal class PassException : Exception
#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore CA1032 // Implement standard exception constructors
    {
    }
}
