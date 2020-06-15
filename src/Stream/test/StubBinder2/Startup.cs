// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Stream.Attributes;
using Steeltoe.Stream.Binder;

[assembly: Binder(Steeltoe.Stream.StubBinder2.StubBinder2.BINDER_NAME, typeof(Steeltoe.Stream.StubBinder2.Startup))]

namespace Steeltoe.Stream.StubBinder2
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
            services.AddSingleton<IBinder, StubBinder2>();
            services.AddSingleton<StubBinder2Dependency>();
        }
    }
}
