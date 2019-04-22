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
            ContainerBuilder container2 = new ContainerBuilder();
            Assert.Throws<ArgumentNullException>(() => container2.RegisterOption<MyOption>(null));
        }

        [Fact]
        public void RegisterOption_Registers_AndBindsConfiguration()
        {
            ContainerBuilder container = new ContainerBuilder();
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
            ContainerBuilder container = new ContainerBuilder();
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
