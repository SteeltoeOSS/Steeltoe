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
using Steeltoe.Management.Endpoint.Security;
using System;
using System.IO;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test
{
    public class AbstractOptionsTest : BaseTest
    {
        [Fact]
        public void InitializedWithDefaults()
        {
            TestOptions2 opts = new TestOptions2();
            Assert.True(opts.Enabled);
            Assert.False(opts.Sensitive);
            Assert.NotNull(opts.Global);
            Assert.False(opts.Global.Enabled.HasValue);
            Assert.False(opts.Global.Sensitive.HasValue);
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
        public void CanSetEnableSensitive()
        {
            TestOptions2 opt2 = new TestOptions2()
            {
                Enabled = false,
                Sensitive = true
            };
            Assert.False(opt2.Enabled);
            Assert.True(opt2.Sensitive);
        }

        [Fact]
        public void BindsConfigurationCorrectly()
        {
            var appsettings = @"
{
    'management': {
        'endpoints': {
            'enabled': false,
            'sensitive': true,
            'path': '/management',
            'info' : {
                'enabled': true,
                'sensitive': false,
                'id': 'infomanagement',
                'requiredPermissions': 'NONE'
            }
        }
    }
}";
            var path = TestHelpers.CreateTempFile(appsettings);
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName);
            var config = configurationBuilder.Build();

            TestOptions2 opts = new TestOptions2("management:endpoints:info", config);

            Assert.NotNull(opts.Global);
            Assert.False(opts.Global.Enabled);
            Assert.True(opts.Global.Sensitive);
            Assert.Equal("/management", opts.Global.Path);

            Assert.True(opts.Enabled);
            Assert.False(opts.Sensitive);
            Assert.Equal("infomanagement", opts.Id);
            Assert.Equal("/management/infomanagement", opts.Path);
            Assert.Equal(Permissions.NONE, opts.RequiredPermissions);
        }

        [Fact]
        public void GlobalSettinsConfigureCorrectly()
        {
            var appsettings = @"
{
    'management': {
        'endpoints': {
            'enabled': false,
            'sensitive': true,
            'path': '/management',
            'info' : {
                'id': 'infomanagement'
            }
        }
    }
}";
            var path = TestHelpers.CreateTempFile(appsettings);
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName);
            var config = configurationBuilder.Build();

            TestOptions2 opts = new TestOptions2("management:endpoints:info", config);

            Assert.NotNull(opts.Global);
            Assert.False(opts.Global.Enabled);
            Assert.True(opts.Global.Sensitive);
            Assert.Equal("/management", opts.Global.Path);

            Assert.False(opts.Enabled);
            Assert.True(opts.Sensitive);
            Assert.Equal("infomanagement", opts.Id);
            Assert.Equal("/management/infomanagement", opts.Path);
        }

        [Fact]
        public void IsAccessAllowed_ReturnsExpected()
        {
            TestOptions2 opt1 = new TestOptions2();
            Assert.True(opt1.IsAccessAllowed(Permissions.FULL));
            Assert.True(opt1.IsAccessAllowed(Permissions.RESTRICTED));
            Assert.True(opt1.IsAccessAllowed(Permissions.NONE));

            TestOptions2 opt2 = new TestOptions2()
            {
                RequiredPermissions = Permissions.NONE
            };
            Assert.True(opt2.IsAccessAllowed(Permissions.FULL));
            Assert.True(opt2.IsAccessAllowed(Permissions.RESTRICTED));
            Assert.True(opt2.IsAccessAllowed(Permissions.NONE));
            Assert.False(opt2.IsAccessAllowed(Permissions.UNDEFINED));

            TestOptions2 opt3 = new TestOptions2()
            {
                RequiredPermissions = Permissions.RESTRICTED
            };
            Assert.True(opt3.IsAccessAllowed(Permissions.FULL));
            Assert.True(opt3.IsAccessAllowed(Permissions.RESTRICTED));
            Assert.False(opt3.IsAccessAllowed(Permissions.NONE));
            Assert.False(opt3.IsAccessAllowed(Permissions.UNDEFINED));

            TestOptions2 opt4 = new TestOptions2()
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
