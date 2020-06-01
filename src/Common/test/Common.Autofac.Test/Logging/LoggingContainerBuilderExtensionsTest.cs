// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
