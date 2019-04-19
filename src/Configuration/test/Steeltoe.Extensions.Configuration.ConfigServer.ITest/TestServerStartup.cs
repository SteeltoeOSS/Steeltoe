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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Extensions.Configuration.ConfigServer.Test;
using System.IO;

namespace Steeltoe.Extensions.Configuration.ConfigServer.ITest
{
    internal class TestServerStartup
    {
        public TestServerStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<ConfigServerDataAsOptions>(Configuration);
            services.AddConfigServerHealthContributor();
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            var config = app.ApplicationServices.GetServices<IConfiguration>();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "VerifyAsInjectedOptions",
                    template: "{controller=Home}/{action=VerifyAsInjectedOptions}");
                routes.MapRoute(
                    name: "Health",
                    template: "{controller=Home}/{action=Health}");
            });
        }
    }
}
