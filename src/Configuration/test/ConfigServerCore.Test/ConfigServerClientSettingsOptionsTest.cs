// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Extensions.Configuration.ConfigServer;
using Steeltoe.Extensions.Configuration.ConfigServer.Test;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServerCore.Test
{
    public class ConfigServerClientSettingsOptionsTest
    {
        [Fact]
        public void ConfigureConfigServerClientSettingsOptions_WithDefaults()
        {
            var services = new ServiceCollection().AddOptions();
            var environment = HostingHelpers.GetHostingEnvironment("Production");

            var builder = new ConfigurationBuilder().AddConfigServer(environment);
            services.AddSingleton<IConfiguration>(services => builder.Build());

            services.ConfigureConfigServerClientOptions();
            var service = services.BuildServiceProvider().GetService<IOptions<ConfigServerClientSettingsOptions>>();
            Assert.NotNull(service);
            var options = service.Value;
            Assert.NotNull(options);
            TestHelper.VerifyDefaults(options.Settings);
        }
    }
}
