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
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Stream.Attributes;
using Steeltoe.Stream.Binder;

[assembly: Binder(Steeltoe.Stream.StubBinder1.StubBinder1.BINDER_NAME, typeof(Steeltoe.Stream.StubBinder1.Startup))]

namespace Steeltoe.Stream.StubBinder1
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public bool ConfigureServicesInvoked { get; set; } = false;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureServicesInvoked = true; // Testing
            var config = Configuration.GetSection("binder1");
            config["name"] = "foobar"; // Unit test checks for this change to verify access to configuration
            services.AddSingleton<IBinder, StubBinder1>();
        }
    }
}
