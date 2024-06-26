// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Steeltoe.Management.Tracing.Test;

public sealed class TracingBaseHostBuilderExtensionsTest : TestBase
{
    [Fact]
    public void AddDistributedTracing_ConfiguresExpectedDefaults()
    {
        var hostBuilder = new HostBuilder();
        IServiceCollection? services = null;

        hostBuilder.ConfigureServices(svc =>
        {
            services = svc;
            svc.AddSingleton(GetConfiguration());
            svc.AddDistributedTracing();
        });

        hostBuilder.Build();
        ServiceProvider serviceProvider = services!.BuildServiceProvider(true);
        ValidateServiceCollectionCommon(serviceProvider);
        ValidateServiceCollectionBase(serviceProvider);
    }
}
