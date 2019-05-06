﻿// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Owin.Builder;
using Owin;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin.Test;
using System;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.Loggers.Test
{
    public class LoggersEndpointAppBuilderExtensionsTest : BaseTest
    {
        [Fact]
        public void UseLoggersActuator_ThrowsIfBuilderNull()
        {
            IAppBuilder builder = null;
            var config = new ConfigurationBuilder().Build();
            var provider = new DynamicLoggerProvider(new ConsoleLoggerSettings());
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseLoggersActuator(config, provider));
            Assert.Equal("builder", exception.ParamName);
        }

        [Fact]
        public void UseLoggersActuator_ThrowsIfConfigNull()
        {
            IAppBuilder builder = new AppBuilder();
            var provider = new DynamicLoggerProvider(new ConsoleLoggerSettings());
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseLoggersActuator(null, provider));
            Assert.Equal("config", exception.ParamName);
        }

        [Fact]
        public void UseLoggersActuator_ThrowsIfProviderNull()
        {
            IAppBuilder builder = new AppBuilder();
            var config = new ConfigurationBuilder().Build();
            var exception = Assert.Throws<ArgumentNullException>(() => builder.UseLoggersActuator(config, null));
            Assert.Equal("loggerProvider", exception.ParamName);
        }
    }
}
