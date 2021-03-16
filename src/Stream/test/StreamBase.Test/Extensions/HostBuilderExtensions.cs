// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Stream.Messaging;
using Steeltoe.Stream.StreamsHost;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Stream.Extensions
{
    public class HostBuilderExtensions
    {
        [Fact]
        public void AddStreamsServices_AddsServices()
        {
            var hostBuilder = Host.CreateDefaultBuilder().
                  AddStreamsServices<SampleSink>();
            var server = hostBuilder
                .AddStreamsServices<SampleSink>()
                .Build();

            var service = server.Services.GetService<IHostedService>();
            Assert.NotNull(service);
        }
    }
}
