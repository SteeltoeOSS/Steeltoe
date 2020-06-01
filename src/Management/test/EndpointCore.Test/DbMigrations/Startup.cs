// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.EndpointBase.DbMigrations;

namespace Steeltoe.Management.Endpoint.DbMigrations.Test
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
            services.AddEntityFrameworkInMemoryDatabase().AddDbContext<MockDbContext>();
            services.AddDbMigrationsActuator(Configuration);
            var helper = Substitute.For<DbMigrationsEndpoint.DbMigrationsEndpointHelper>();
            helper.GetPendingMigrations(Arg.Any<DbContext>()).Returns(new[] { "pending" });
            helper.GetAppliedMigrations(Arg.Any<DbContext>()).Returns(new[] { "applied" });
            helper.ScanRootAssembly.Returns(typeof(MockDbContext).Assembly);
            services.AddSingleton(helper);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDbMigrationsActuator();
        }
    }
}
