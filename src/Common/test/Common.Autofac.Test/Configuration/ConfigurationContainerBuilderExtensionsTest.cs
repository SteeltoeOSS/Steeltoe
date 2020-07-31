// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using System;
using Xunit;

namespace Steeltoe.Common.Configuration.Autofac.Test
{
    public class ConfigurationContainerBuilderExtensionsTest
    {
        [Fact]
        public void RegisterConfiguration_ThrowsNulls()
        {
            ContainerBuilder container = null;
            Assert.Throws<ArgumentNullException>(() => container.RegisterConfiguration(null));
            var container2 = new ContainerBuilder();
            Assert.Throws<ArgumentNullException>(() => container2.RegisterConfiguration(null));
        }

        [Fact]
        public void RegisterConfiguration_Registers()
        {
            var container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();
            container.RegisterConfiguration(config);

            var built = container.Build();
            Assert.True(built.IsRegistered<IConfigurationRoot>());
            Assert.True(built.IsRegistered<IConfiguration>());
        }
    }
}
