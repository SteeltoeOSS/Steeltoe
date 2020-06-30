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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Config;
using Xunit;

namespace Steeltoe.Stream.Extensions
{
    public class BinderServicesExtensionsTest
    {
        [Fact]
        public void AddBinderServices_AddsServices()
        {
            var container = new ServiceCollection();
            container.AddOptions();
            container.AddLogging((b) => b.AddConsole());
            var config = new ConfigurationBuilder().Build();
            container.AddSingleton<IConfiguration>(config);
            container.AddSingleton<IApplicationContext, GenericApplicationContext>();
            container.AddBinderServices(config);
            var serviceProvider = container.BuildServiceProvider();

            Assert.NotNull(serviceProvider.GetService<IBinderFactory>());
            Assert.NotNull(serviceProvider.GetService<IBinderTypeRegistry>());
            Assert.NotNull(serviceProvider.GetService<IBinderConfigurations>());
        }
    }
}
