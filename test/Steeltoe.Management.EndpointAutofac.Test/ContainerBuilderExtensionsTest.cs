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

using Autofac;
using Autofac.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Owin;
using System;
using System.Linq;
using System.Web;
using Xunit;

namespace Steeltoe.Management.EndpointAutofac.Test
{
    public class ContainerBuilderExtensionsTest
    {
        [Fact]
        public void UseCloudFoundryMiddlewares_ThrowsOnNulls()
        {
            //// Arrange
            //ContainerBuilder containerNull = null;
            //ContainerBuilder container = new ContainerBuilder();
            //IConfigurationRoot configNull = null;
            //IConfigurationRoot config = new ConfigurationBuilder().Build();

            //// Act
            //var ex = Assert.Throws<ArgumentNullException>(() => ContainerBuilderExtensions.RegisterCloudFoundryActuators(containerNull, config));
            //var ex2 = Assert.Throws<ArgumentNullException>(() => ContainerBuilderExtensions.RegisterCloudFoundryActuators(container, configNull));

            //// Assert
            //Assert.Equal("container", ex.ParamName);
            //Assert.Equal("config", ex2.ParamName);
        }

        [Fact]
        public void UseCloudFoundryMiddlewares_AddsExpectedNumberOfMiddlewares()
        {
            //// Arrange
            //ContainerBuilder containerBuilder = new ContainerBuilder();
            //IConfigurationRoot config = new ConfigurationBuilder().Build();

            //// Act
            //ContainerBuilderExtensions.RegisterCloudFoundryActuators(containerBuilder, config);
            //var container = containerBuilder.Build();
            //var middlewares = container.ComponentRegistry.Registrations.SelectMany(r => r.Services)
            //    .OfType<TypedService>()
            //    .Where(s => s.ServiceType.IsAssignableTo<OwinMiddleware>());

            //// Assert
            //// TODO: adjust UseCloudFoundryMiddlewares for consistency with other "add/use cloud foundry" actuator methods
            //Assert.Equal(12, middlewares.Count());
        }

        ////[Fact]
        ////public void RegisterCloudFoundryModules_ThrowsOnNulls()
        ////{
        ////    // Arrange
        ////    ContainerBuilder containerNull = null;
        ////    ContainerBuilder container = new ContainerBuilder();
        ////    IConfigurationRoot configNull = null;
        ////    IConfigurationRoot config = new ConfigurationBuilder().Build();

        ////    // Act
        ////    var ex = Assert.Throws<ArgumentNullException>(() => ContainerBuilderExtensions.RegisterCloudFoundryModules(containerNull, config));
        ////    var ex2 = Assert.Throws<ArgumentNullException>(() => ContainerBuilderExtensions.RegisterCloudFoundryModules(container, configNull));

        ////    // Assert
        ////    Assert.Equal("container", ex.ParamName);
        ////    Assert.Equal("config", ex2.ParamName);
        ////}

        ////[Fact]
        ////public void RegisterCloudFoundryModules_AddsExpectedNumberOfHttpModules()
        ////{
        ////    // Arrange
        ////    ContainerBuilder containerBuilder = new ContainerBuilder();
        ////    IConfigurationRoot config = new ConfigurationBuilder().Build();

        ////    // Act
        ////    ContainerBuilderExtensions.RegisterCloudFoundryModules(containerBuilder, config);
        ////    var container = containerBuilder.Build();
        ////    var modules = container.ComponentRegistry.Registrations.SelectMany(r => r.Services)
        ////        .OfType<TypedService>()
        ////        .Where(s => s.ServiceType.IsAssignableTo<IHttpModule>());

        ////    // Assert
        ////    // TODO: adjust UseCloudFoundryMiddlewares for consistency with other "add/use cloud foundry" actuator methods
        ////    Assert.Equal(11, modules.Count());
        ////}
    }
}
