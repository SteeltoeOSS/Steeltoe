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

namespace Steeltoe.Extensions.Configuration.Placeholder.Test
{
    public class TestServerStartup
    {
        public static IServiceProvider ServiceProvider { get; set; }

        private IConfiguration _configuration;

        public TestServerStartup(IConfiguration config)
        {
            _configuration = config;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.ConfigurePlaceholderResolver(_configuration);
        }

        public void Configure(IApplicationBuilder app)
        {
            ServiceProvider = app.ApplicationServices;
        }
    }
}
