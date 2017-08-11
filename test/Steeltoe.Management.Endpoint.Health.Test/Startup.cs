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

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Info;
using System.IO;


namespace Steeltoe.Management.Endpoint.Health.Test
{
    public class Startup
    {
        public IConfiguration Configuration { get; set; }
        public Startup()
        {
            var appsettings = @"
{
    'management': {
        'endpoints': {
            'enabled': true,
            'sensitive': false,
            'path': '/cloudfoundryapplication',
            'health' : {
                'enabled': true,
                'sensitive': false
            }
        }
    }
}";
            var path = TestHelpers.CreateTempFile(appsettings);
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName);
            Configuration = configurationBuilder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHealthActuator(Configuration);
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
     
            app.UseHealthActuator();
            app.UseMvc();
        }
    }
}
