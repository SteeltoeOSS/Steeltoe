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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Test.EntityFramework;
using Steeltoe.Management.EndpointBase.DbMigrations;

namespace Steeltoe.Management.Endpoint.EntityFramework.Test
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<MockDbContext>();
            services.AddCloudFoundryActuator(Configuration);
            services.AddEntityFrameworkActuator(Configuration, builder => builder.AddDbContext<MockDbContext>());
            var helper = Substitute.For<EntityFrameworkEndpoint.EntityFrameworkEndpointHelper>();
            helper.GetPendingMigrations(Arg.Any<DbContext>()).Returns(new[] { "pending" });
            helper.GetAppliedMigrations(Arg.Any<DbContext>()).Returns(new[] { "applied" });
            helper.ScanRootAssembly.Returns(typeof(MockDbContext).Assembly);
            services.AddSingleton(helper);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseEntityFrameworkActuator();
        }
    }
}
