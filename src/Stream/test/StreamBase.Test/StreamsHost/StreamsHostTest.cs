// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Stream.Attributes;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Messaging;
using System;
using Xunit;

namespace Steeltoe.Stream.StreamsHost
{
    public class StreamsHostTest
    {
        [Fact]
        public void HostCanBeStarted()
        {
            FakeHostedService service;
            using (var host = StreamsHost.CreateDefaultBuilder<SampleSink>()
                .ConfigureServices(svc => svc.AddSingleton<IHostedService, FakeHostedService>())
                .Start())
            {
                Assert.NotNull(host);
                service = (FakeHostedService)host.Services.GetRequiredService<IHostedService>();
                Assert.NotNull(service);
                Assert.Equal(1, service.StartCount);
                Assert.Equal(0, service.StopCount);
                Assert.Equal(0, service.DisposeCount);
            }

            Assert.Equal(1, service.StartCount);
            Assert.Equal(0, service.StopCount);
            Assert.Equal(1, service.DisposeCount);
        }

        [Fact]
        public void HostSetsupSpringBootConfigSource()
        {
            Environment.SetEnvironmentVariable("SPRING_APPLICATION_JSON", "{\"spring.cloud.stream.bindings.input.destination\":\"foobar\",\"spring.cloud.stream.bindings.output.destination\":\"barfoo\"}");
            using (var host = StreamsHost.CreateDefaultBuilder<SampleSink>()
                .Start())
            {
                // service = (FakeHostedService)host.Services.GetRequiredService<IHostedService>();
                var rabbitBindingsOptions = host.Services.GetService<IOptionsMonitor<BindingServiceOptions>>();
                Assert.NotNull(rabbitBindingsOptions);
                Assert.Equal("foobar", rabbitBindingsOptions.CurrentValue.Bindings["input"].Destination);
                Assert.Equal("barfoo", rabbitBindingsOptions.CurrentValue.Bindings["output"].Destination);
            }
        }
    }

    [EnableBinding(typeof(ISink))]
    public class SampleSink
    {
        [StreamListener("input")]
        public void HandleInputMessage(string foo)
        {
        }
    }
}
