// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Tracing.Test;

public class TracingCoreServiceCollectionExtensionsTest : TestBase
{
    [Fact]
    public void AddDistributedTracingAspNetCore_ThrowsOnNulls()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => TracingCoreServiceCollectionExtensions.AddDistributedTracingAspNetCore(null));
        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void AddDistributedTracingAspNetCore_ConfiguresExpectedDefaults()
    {
        var services = new ServiceCollection().AddSingleton(GetConfiguration());

        var serviceProvider = services.AddDistributedTracingAspNetCore().BuildServiceProvider();

        ValidateServiceCollectionCommon(serviceProvider);
        ValidateServiceContainerCore(serviceProvider);
    }

    // this test should find OTLP exporter is configured, see TracingBase.Test for Zipkin & Jaeger
    [Fact]
    public void AddDistributedTracingAspNetCore_WiresIncludedExporters()
    {
        var services = new ServiceCollection().AddSingleton(GetConfiguration());

        var serviceProvider = services.AddDistributedTracing(null).BuildServiceProvider();
        var hst = serviceProvider.GetService<IHostedService>();
        Assert.NotNull(hst);
        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);

        Assert.NotNull(serviceProvider.GetService<IOptions<OtlpExporterOptions>>());
    }

    [Fact]
    public void AddDistributedTracingAspNetCore_WiresWavefrontExporters()
    {
        var services = new ServiceCollection()
            .AddSingleton(GetConfiguration(new Dictionary<string, string>
            {
                { "management:metrics:export:wavefront:uri", "https://test.wavefront.com" },
                { "management:metrics:export:wavefront:apiToken", "fakeSecret" }
            }));

        var serviceProvider = services.AddDistributedTracing(null).BuildServiceProvider();

        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);
    }
}
