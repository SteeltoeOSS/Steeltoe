﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
