//
// Copyright 2015 the original author or authors.
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


using Microsoft.AspNet.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNet.Hosting;
using Spring.Extensions.Configuration.CloudFoundry;
using Microsoft.Extensions.DependencyInjection;

namespace Spring.Extensions.Configuration.Server.IntegrationTest
{
    public class TestServerCloudfoundryStartup
    {
        public IConfiguration Configuration { get; set; }


        public TestServerCloudfoundryStartup(IHostingEnvironment environment)
        {

            var builder = new ConfigurationBuilder()
                .AddCloudFoundry()
                .AddConfigServer(environment);
            Configuration = builder.Build();

        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<ConfigServerDataAsOptions>(Configuration);
            services.AddMvc();
        }
        public void Configure(IApplicationBuilder app)
        {

            app.UseMvc(routes =>
                routes.MapRoute(
                    name: "VerifyAsInjectedOptions",
                    template: "{controller=Home}/{action=VerifyAsInjectedOptions}")
                );
        }
    }
}
