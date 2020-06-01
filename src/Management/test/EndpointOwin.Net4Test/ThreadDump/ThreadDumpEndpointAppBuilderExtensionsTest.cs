// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Owin.Builder;
using Owin;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.Endpoint.ThreadDump;
using System;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.ThreadDump.Test
{
    public class ThreadDumpEndpointAppBuilderExtensionsTest : BaseTest
    {
        [Fact]
        public void UseThreadDumpActuator_ThrowsIfBuilderNull()
        {
            IAppBuilder builder = null;
            var config = new ConfigurationBuilder().Build();
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseThreadDumpActuator(config));
            Assert.Equal("builder", exception.ParamName);
        }

        [Fact]
        public void UseThreadDumpActuator_ThrowsIfConfigNull()
        {
            IAppBuilder builder = new AppBuilder();
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseThreadDumpActuator(null));
            Assert.Equal("config", exception.ParamName);
        }

        [Fact]
        public void UseThreadDumpActuator_ThrowsIfOptionsNull()
        {
            IAppBuilder builder = new AppBuilder();
            var threadDumper = new ThreadDumper(new ThreadDumpEndpointOptions());
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseThreadDumpActuator(null, threadDumper));
            Assert.Equal("options", exception.ParamName);
        }

        [Fact]
        public void UseThreadDumpActuator_ThrowsIfDumperNull()
        {
            IAppBuilder builder = new AppBuilder();
            var config = new ConfigurationBuilder().Build();
            var options = new ThreadDumpEndpointOptions(config);
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseThreadDumpActuator(options, null));
            Assert.Equal("threadDumper", exception.ParamName);
        }
    }
}
