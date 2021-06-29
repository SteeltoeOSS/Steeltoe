// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Messaging;
using Steeltoe.Stream.StreamHost;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Stream.Extensions
{
    public class HostBuilderExtensionsTest
    {
        [Fact]
        public void HostBuilderExtensionTest()
        {
            var hostBuilder = Host.CreateDefaultBuilder().AddStreamServices<SampleSink>();
            Assert.NotNull(hostBuilder);
        }
    }
}
