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
using Microsoft.Extensions.Options;
using Steeltoe.Common.Options.Autofac;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Extensions.Configuration.CloudFoundryAutofac.Test
{
    public class CloudFoundryContainerBuilderExtensionsTest
    {
        [Fact]
        public void RegisterCloudFoundryOptions_ThrowsNulls()
        {
            ContainerBuilder container = null;
            Assert.Throws<ArgumentNullException>(() => container.RegisterCloudFoundryOptions(null));
            ContainerBuilder container2 = new ContainerBuilder();
            Assert.Throws<ArgumentNullException>(() => container2.RegisterCloudFoundryOptions(null));
        }

        [Fact]
        public void RegisterCloudFoundryOptions_Registers_AndBindsConfiguration()
        {
            ContainerBuilder container = new ContainerBuilder();
            var dict = new Dictionary<string, string>()
            {
                { "vcap:application:cf_api", "http://foo.bar/foo" },
                { "vcap:application:application_id", "application_id" },
                { "vcap:application:application_name", "application_name" },
                { "vcap:services:p-config-server:0:name", "myConfigServer" },
                { "vcap:services:p-config-server:0:plan", "myPlan" },
                { "vcap:services:p-config-server:0:label", "myLabel" }
            };

            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
            container.RegisterOptions();

            container.RegisterCloudFoundryOptions(config);
            var built = container.Build();

            var service = built.Resolve<IOptions<CloudFoundryApplicationOptions>>();
            Assert.NotNull(service);
            Assert.NotNull(service.Value);
            Assert.Equal("http://foo.bar/foo", service.Value.CF_Api);
            Assert.Equal("application_name", service.Value.ApplicationName);
            Assert.Equal("application_id", service.Value.ApplicationId);

            var services = built.Resolve<IOptions<CloudFoundryServicesOptions>>();
            Assert.NotNull(services);
            Assert.NotNull(services.Value);
            var s = services.Value.Services["p-config-server"];
            Assert.NotNull(s);
            Assert.NotEmpty(s);
            var svc = s[0];
            Assert.Equal("myLabel", svc.Label);
        }
    }
}
