// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Microsoft.Owin.Builder;
using Owin;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.EndpointOwin.Test;
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
            var threadDumper = new ThreadDumper(new ThreadDumpOptions());
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseThreadDumpActuator(null, threadDumper));
            Assert.Equal("options", exception.ParamName);
        }

        [Fact]
        public void UseThreadDumpActuator_ThrowsIfDumperNull()
        {
            IAppBuilder builder = new AppBuilder();
            var config = new ConfigurationBuilder().Build();
            var options = new ThreadDumpOptions(config);
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseThreadDumpActuator(options, null));
            Assert.Equal("threadDumper", exception.ParamName);
        }
    }
}
