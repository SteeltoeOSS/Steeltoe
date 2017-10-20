//
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
//

using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Logging.Autofac;
using Steeltoe.Common.Options.Autofac;
using System;
using Xunit;

namespace Steeltoe.Common.Autofac.Test.Logging
{
    public class LoggingContainerBuilderExtensionsTest
    {
        [Fact]
        public void RegisterLogging_ThrowsNulls()
        {
            ContainerBuilder container = null;
            Assert.Throws<ArgumentNullException>(() => container.RegisterLogging(null));
            ContainerBuilder container2 = new ContainerBuilder();
            Assert.Throws<ArgumentNullException>(() => container2.RegisterLogging(null));
        }

        [Fact]
        public void RegisterLogging_Registers()
        {
            ContainerBuilder container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();

            container.RegisterOptions();
            container.RegisterLogging(config);
            container.RegisterConsoleLogging();

            var built = container.Build();
            var fac = built.Resolve<ILoggerFactory>();
            Assert.NotNull(fac);
            var logger = built.Resolve<ILogger<LoggingContainerBuilderExtensionsTest>>();
            Assert.NotNull(logger);

        }
    }
}
