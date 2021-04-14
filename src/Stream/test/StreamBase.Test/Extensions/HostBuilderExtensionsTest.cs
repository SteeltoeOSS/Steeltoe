// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Messaging;
using Steeltoe.Stream.StreamsHost;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Stream.Extensions
{
    public class HostBuilderExtensionsTest
    {
        [Fact]
        public void AddStreamsServices_AddsServices()
        {
            var hostBuilder = Host.CreateDefaultBuilder().AddStreamsServices<SampleSink>();
            var server = hostBuilder.Build();

            var service = server.Services.GetService<IHostedService>();
            Assert.NotNull(service);
        }

        [Fact(Skip="Figure out why")]
        public void AddStreamsServices_AddsSpringBootEnv()
        {
            Environment.SetEnvironmentVariable("SPRING_APPLICATION_JSON", "{\"spring.cloud.stream.bindings.input.destination\":\"foobar\",\"spring.cloud.stream.bindings.output.destination\":\"barfoo\"}");
            var hostBuilder = Host.CreateDefaultBuilder().AddStreamsServices<SampleSink>();
            var server = hostBuilder.Build();

            var rabbitBindingsOptions = server.Services.GetService<IOptionsMonitor<BindingServiceOptions>>();
            Assert.NotNull(rabbitBindingsOptions);
            Assert.Equal("foobar", rabbitBindingsOptions.CurrentValue.Bindings["input"].Destination);
            Assert.Equal("barfoo", rabbitBindingsOptions.CurrentValue.Bindings["output"].Destination);
        }
    }
}
