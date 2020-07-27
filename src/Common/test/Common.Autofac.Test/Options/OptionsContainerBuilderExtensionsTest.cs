// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Common.Options.Autofac.Test
{
    public class OptionsContainerBuilderExtensionsTest
    {
        [Fact]
        public void RegisterOptions_ThrowsNulls()
        {
            ContainerBuilder container = null;
            Assert.Throws<ArgumentNullException>(() => container.RegisterOptions());
        }

        [Fact]
        public void RegisterOption_ThrowsNulls()
        {
            ContainerBuilder container = null;
            Assert.Throws<ArgumentNullException>(() => container.RegisterOption<MyOption>(null));
            var container2 = new ContainerBuilder();
            Assert.Throws<ArgumentNullException>(() => container2.RegisterOption<MyOption>(null));
        }

        [Fact]
        public void RegisterOption_Registers_AndBindsConfiguration()
        {
            var container = new ContainerBuilder();
            var dict = new Dictionary<string, string>()
            {
                { "f1", "foobar" }
            };

            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
            container.RegisterOptions();

            container.RegisterOption<MyOption>(config);
            var built = container.Build();

            var service = built.Resolve<IOptions<MyOption>>();
            Assert.NotNull(service);
            Assert.NotNull(service.Value);
            Assert.Equal("foobar", service.Value.F1);
        }

        [Fact]
        public void RegisterPostConfigure_Registers_RunsPostAction()
        {
            var container = new ContainerBuilder();
            var dict = new Dictionary<string, string>()
            {
                { "f1", "foobar" }
            };

            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
            container.RegisterOptions();

            container.RegisterOption<MyOption>(config);
            container.RegisterPostConfigure<MyOption>((opt) =>
            {
                opt.F1 = "changed";
            });

            var built = container.Build();

            var service = built.Resolve<IOptions<MyOption>>();
            Assert.NotNull(service);
            Assert.NotNull(service.Value);
            Assert.Equal("changed", service.Value.F1);
        }

        private class MyOption
        {
            public string F1 { get; set; }
        }
    }
}
