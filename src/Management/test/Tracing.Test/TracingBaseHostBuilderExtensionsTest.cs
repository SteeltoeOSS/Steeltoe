// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Management.Tracing.Test;

public sealed class TracingBaseHostBuilderExtensionsTest : TestBase
{
    [Fact]
    public void AddDistributedTracing_ConfiguresExpectedDefaults()
    {
        HostBuilder hostBuilder = TestHostBuilderFactory.Create();

        hostBuilder.ConfigureServices(services =>
        {
            services.AddSingleton(GetConfiguration());
            services.AddDistributedTracing();
        });

        using IHost host = hostBuilder.Build();

        ValidateServiceCollectionCommon(host.Services);
        ValidateServiceCollectionBase(host.Services);
    }
}
