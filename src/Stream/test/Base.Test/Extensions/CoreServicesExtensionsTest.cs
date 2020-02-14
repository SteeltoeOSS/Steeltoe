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
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Converter;
using Steeltoe.Common.Expression;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging.Core;
using Xunit;

namespace Steeltoe.Stream.Extensions
{
    public class CoreServicesExtensionsTest
    {
        [Fact]
        public void AddCoreServices_AddsServices()
        {
            var container = new ServiceCollection();
            container.AddOptions();
            container.AddLogging((b) => b.AddConsole());
            var config = new ConfigurationBuilder().Build();
            container.AddCoreServices();
            var serviceProvider = container.BuildServiceProvider();
            Assert.NotNull(serviceProvider.GetService<IConversionService>());
            Assert.NotNull(serviceProvider.GetService<ILifecycleProcessor>());
            Assert.NotNull(serviceProvider.GetService<IDestinationRegistry>());
            Assert.NotNull(serviceProvider.GetService<IExpressionParser>());
            Assert.NotNull(serviceProvider.GetService<IEvaluationContext>());
        }
    }
}
