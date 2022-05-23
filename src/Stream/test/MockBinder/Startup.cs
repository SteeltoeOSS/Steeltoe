// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Steeltoe.Stream.Attributes;
using Steeltoe.Stream.Binder;

[assembly: Binder("mock", typeof(Steeltoe.Stream.MockBinder.Startup))]

namespace Steeltoe.Stream.MockBinder
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var mock = new Mock<IBinder<object>>() { DefaultValue = DefaultValue.Mock };
            mock.Setup((b) => b.ServiceName).Returns("mock");
            services.AddSingleton(mock.Object);
            services.AddSingleton<IBinder>((p) => p.GetRequiredService<IBinder<object>>());
        }
    }
}
