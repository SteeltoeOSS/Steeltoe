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
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test
{
    public class EurekaPostConfigurerTest
    {
        [Fact]
        public void UpdateConfiguration_WithInstDefaults_UpdatesCorrectly()
        {
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                { "spring:application:name", "bar" },
                { "spring:application:instance_id", "instance" },
                { "spring:cloud:discovery:registrationMethod", "registrationMethod" },
            });

            IConfigurationRoot root = builder.Build();

            var instOpts = new EurekaInstanceOptions();
            EurekaPostConfigurer.UpdateConfiguration(root, instOpts);

            Assert.Equal("bar", instOpts.AppName);
            Assert.Equal("instance", instOpts.InstanceId);
            Assert.Equal("registrationMethod", instOpts.RegistrationMethod);
            Assert.Equal("bar", instOpts.VirtualHostName);
            Assert.Equal("bar", instOpts.SecureVirtualHostName);
        }

        [Fact]
        public void UpdateConfiguration_UpdatesCorrectly()
        {
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                { "spring:application:name", "bar" },
                { "spring:application:instance_id", "instance" },
                { "spring:cloud:discovery:registrationMethod", "registrationMethod" },
            });

            IConfigurationRoot root = builder.Build();

            var instOpts = new EurekaInstanceOptions()
            {
                AppName = "dontChange",
                InstanceId = "dontChange",
                RegistrationMethod = "dontChange"
            };

            EurekaPostConfigurer.UpdateConfiguration(root, instOpts);

            Assert.Equal("dontChange", instOpts.AppName);
            Assert.Equal("dontChange", instOpts.InstanceId);
            Assert.Equal("dontChange", instOpts.RegistrationMethod);
            Assert.Equal("dontChange", instOpts.VirtualHostName);
            Assert.Equal("dontChange", instOpts.SecureVirtualHostName);
        }

        [Fact]
        public void UpdateConfiguration_UpdatesCorrectly2()
        {
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                { "spring:application:name", "bar" },
                { "spring:cloud:discovery:registrationMethod", "registrationMethod" },
            });

            IConfigurationRoot root = builder.Build();

            var instOpts = new EurekaInstanceOptions();

            EurekaPostConfigurer.UpdateConfiguration(root, instOpts);

            Assert.Equal("bar", instOpts.AppName);
            Assert.EndsWith("bar:80", instOpts.InstanceId, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("registrationMethod", instOpts.RegistrationMethod);
            Assert.Equal("bar", instOpts.VirtualHostName);
            Assert.Equal("bar", instOpts.SecureVirtualHostName);
        }

        [Fact]
        public void UpdateConfiguration_UpdatesCorrectly3()
        {
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                { "spring:application:instance_id", "instance" },
                { "spring:cloud:discovery:registrationMethod", "registrationMethod" },
            });

            IConfigurationRoot root = builder.Build();

            var instOpts = new EurekaInstanceOptions()
            {
                AppName = "dontChange",
                InstanceId = "dontChange",
                RegistrationMethod = "dontChange"
            };

            EurekaPostConfigurer.UpdateConfiguration(root, instOpts);

            Assert.Equal("dontChange", instOpts.AppName);
            Assert.Equal("dontChange", instOpts.InstanceId);
            Assert.Equal("dontChange", instOpts.RegistrationMethod);
            Assert.Equal("dontChange", instOpts.VirtualHostName);
            Assert.Equal("dontChange", instOpts.SecureVirtualHostName);
        }
    }
}
