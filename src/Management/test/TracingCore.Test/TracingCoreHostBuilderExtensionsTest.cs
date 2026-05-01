// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Steeltoe.Management.Tracing.Test;

public class TracingCoreHostBuilderExtensionsTest : TestBase
{
    [Fact]
    public void AddDistributedTracingAspNetCore_ConfiguresExpectedDefaults()
    {
        using var host = new HostBuilder()
            .ConfigureServices(svc => svc.AddSingleton(GetConfiguration()))
            .AddDistributedTracingAspNetCore()
            .Build();

        ValidateServiceCollectionCommon(host.Services);
        ValidateServiceContainerCore(host.Services);
    }
}