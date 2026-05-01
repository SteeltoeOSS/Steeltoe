// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Xunit;

namespace Steeltoe.Management.Tracing.Test;

public class TracingBaseHostBuilderExtensionsTest : TestBase
{
    [Fact]
    public void AddDistributedTracing_ThrowsOnNulls()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => TracingBaseServiceCollectionExtensions.AddDistributedTracing(null));
        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void AddDistributedTracing_ConfiguresExpectedDefaults()
    {
        var hostBuilder = new HostBuilder();
        hostBuilder.ConfigureServices(svc =>
        {
            svc.AddSingleton(GetConfiguration());
            svc.AddDistributedTracing();
        });

        using var host = hostBuilder.Build();
        ValidateServiceCollectionCommon(host.Services);
        ValidateServiceCollectionBase(host.Services);
    }
}