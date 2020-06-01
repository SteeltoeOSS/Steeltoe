// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
                { "vcap:application:cf_api", "https://foo.bar/foo" },
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
            Assert.Equal("https://foo.bar/foo", service.Value.CF_Api);
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
