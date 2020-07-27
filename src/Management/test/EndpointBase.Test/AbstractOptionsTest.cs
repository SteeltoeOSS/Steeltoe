// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Security;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test
{
    public class AbstractOptionsTest : BaseTest
    {
        [Fact]
        public void InitializedWithDefaults()
        {
            var opts = new TestOptions2();
            Assert.True(opts.Enabled);
            Assert.NotNull(opts.Global);
            Assert.False(opts.Global.Enabled.HasValue);
            Assert.Equal("/", opts.Global.Path);
            Assert.Equal(Permissions.UNDEFINED, opts.RequiredPermissions);
        }

        [Fact]
        public void ThrowsIfSectionNameNull()
        {
            Assert.Throws<ArgumentNullException>(() => new TestOptions2(null, null));
        }

        [Fact]
        public void ThrowsIfConfigNull()
        {
            IConfiguration config = null;
            Assert.Throws<ArgumentNullException>(() => new TestOptions2("foobar", config));
        }

        [Fact]
        public void CanSetEnable()
        {
            var opt2 = new TestOptions2()
            {
                Enabled = false,
            };
            Assert.False(opt2.Enabled);
        }

        [Fact]
        public void BindsConfigurationCorrectly()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:path"] = "/management",
                ["management:endpoints:info:enabled"] = "true",
                ["management:endpoints:info:id"] = "infomanagement",
                ["management:endpoints:info:requiredPermissions"] = "NONE"
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var opts = new TestOptions2("management:endpoints:info", config);

            Assert.True(opts.Enabled);
            Assert.Equal("infomanagement", opts.Id);
            Assert.Equal("/management/infomanagement", opts.Path);
            Assert.Equal(Permissions.NONE, opts.RequiredPermissions);
        }

        [Fact]
        public void GlobalSettinsConfigureCorrectly()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:path"] = "/management",
                ["management:endpoints:info:id"] = "infomanagement"
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var opts = new TestOptions2("management:endpoints:info", config);

            Assert.NotNull(opts.Global);
            Assert.False(opts.Global.Enabled);
            Assert.Equal("/management", opts.Global.Path);

            Assert.False(opts.Enabled);
            Assert.Equal("infomanagement", opts.Id);
            Assert.Equal("/management/infomanagement", opts.Path);
        }

        [Fact]
        public void IsAccessAllowed_ReturnsExpected()
        {
            var opt1 = new TestOptions2();
            Assert.True(opt1.IsAccessAllowed(Permissions.FULL));
            Assert.True(opt1.IsAccessAllowed(Permissions.RESTRICTED));
            Assert.True(opt1.IsAccessAllowed(Permissions.NONE));

            var opt2 = new TestOptions2()
            {
                RequiredPermissions = Permissions.NONE
            };
            Assert.True(opt2.IsAccessAllowed(Permissions.FULL));
            Assert.True(opt2.IsAccessAllowed(Permissions.RESTRICTED));
            Assert.True(opt2.IsAccessAllowed(Permissions.NONE));
            Assert.False(opt2.IsAccessAllowed(Permissions.UNDEFINED));

            var opt3 = new TestOptions2()
            {
                RequiredPermissions = Permissions.RESTRICTED
            };
            Assert.True(opt3.IsAccessAllowed(Permissions.FULL));
            Assert.True(opt3.IsAccessAllowed(Permissions.RESTRICTED));
            Assert.False(opt3.IsAccessAllowed(Permissions.NONE));
            Assert.False(opt3.IsAccessAllowed(Permissions.UNDEFINED));

            var opt4 = new TestOptions2()
            {
                RequiredPermissions = Permissions.FULL
            };
            Assert.True(opt4.IsAccessAllowed(Permissions.FULL));
            Assert.False(opt4.IsAccessAllowed(Permissions.RESTRICTED));
            Assert.False(opt4.IsAccessAllowed(Permissions.NONE));
            Assert.False(opt4.IsAccessAllowed(Permissions.UNDEFINED));
        }
    }
}
