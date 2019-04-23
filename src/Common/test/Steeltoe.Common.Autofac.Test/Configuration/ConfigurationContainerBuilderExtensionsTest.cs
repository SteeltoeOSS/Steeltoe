// Copyright 2017 the original author or authors.
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
            ContainerBuilder container2 = new ContainerBuilder();
            Assert.Throws<ArgumentNullException>(() => container2.RegisterConfiguration(null));
        }

        [Fact]
        public void RegisterConfiguration_Registers()
        {
            ContainerBuilder container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();
            container.RegisterConfiguration(config);

            var built = container.Build();
            Assert.True(built.IsRegistered<IConfigurationRoot>());
            Assert.True(built.IsRegistered<IConfiguration>());
        }
    }
}
