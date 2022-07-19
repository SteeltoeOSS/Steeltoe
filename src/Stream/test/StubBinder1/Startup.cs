// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Stream.Attributes;
using Steeltoe.Stream.Binder;

[assembly: Binder(Steeltoe.Stream.StubBinder1.StubBinder1.BinderName, typeof(Steeltoe.Stream.StubBinder1.Startup))]

namespace Steeltoe.Stream.StubBinder1;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public bool ConfigureServicesInvoked { get; set; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        ConfigureServicesInvoked = true; // Testing
        var config = Configuration.GetSection("binder1");
        config["name"] = "foobar"; // Unit test checks for this change to verify access to configuration
        services.AddSingleton<IBinder, StubBinder1>();
    }
}
