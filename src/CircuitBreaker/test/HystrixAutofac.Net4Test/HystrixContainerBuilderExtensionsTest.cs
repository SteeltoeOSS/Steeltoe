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
using System;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.Autofac.Test
{
    public class HystrixContainerBuilderExtensionsTest
    {
        private IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("DummyCommand");
        private IHystrixCommandKey commandKey = HystrixCommandKeyDefault.AsKey("DummyCommand");

        [Fact]
        public void RegisterHystrixCommand_ThrowsIfContainerBuilderNull()
        {
            ContainerBuilder container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();
            string stringKey = "DummyCommand";

            var ex = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<DummyCommand>(null, groupKey, null));
            Assert.Contains(nameof(container), ex.Message);
            var ex2 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<IDummyCommand, DummyCommand>(null, groupKey, null));
            Assert.Contains(nameof(container), ex2.Message);
            var ex3 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<DummyCommand>(null, stringKey, null));
            Assert.Contains(nameof(container), ex3.Message);
            var ex4 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<IDummyCommand, DummyCommand>(null, stringKey, null));
            Assert.Contains(nameof(container), ex4.Message);
            var ex5 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<DummyCommand>(null, groupKey, commandKey, null));
            Assert.Contains(nameof(container), ex5.Message);
            var ex6 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<IDummyCommand, DummyCommand>(null, groupKey, commandKey, null));
            Assert.Contains(nameof(container), ex6.Message);
            var ex7 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<DummyCommand>(null, stringKey, stringKey, null));
            Assert.Contains(nameof(container), ex7.Message);
            var ex8 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<IDummyCommand, DummyCommand>(null, stringKey, stringKey, null));
            Assert.Contains(nameof(container), ex8.Message);
        }

        [Fact]
        public void RegisterHystrixCommand_ThrowsIfGroupKeyNull()
        {
            ContainerBuilder services = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();
            string stringKey = "DummyCommand";

            var ex = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<DummyCommand>(services, (IHystrixCommandGroupKey)null, null));
            Assert.Contains(nameof(groupKey), ex.Message);
            var ex2 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<IDummyCommand, DummyCommand>(services, (IHystrixCommandGroupKey)null, null));
            Assert.Contains(nameof(groupKey), ex2.Message);
            var ex3 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<DummyCommand>(services, (string)null, null));
            Assert.Contains(nameof(groupKey), ex3.Message);
            var ex4 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<IDummyCommand, DummyCommand>(services, (string)null, null));
            Assert.Contains(nameof(groupKey), ex4.Message);
            var ex5 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<DummyCommand>(services, null, commandKey, null));
            Assert.Contains(nameof(groupKey), ex5.Message);
            var ex6 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<IDummyCommand, DummyCommand>(services, null, commandKey, null));
            Assert.Contains(nameof(groupKey), ex6.Message);
            var ex7 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<DummyCommand>(services, null, stringKey, null));
            Assert.Contains(nameof(groupKey), ex7.Message);
            var ex8 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<IDummyCommand, DummyCommand>(services, null, stringKey, null));
            Assert.Contains(nameof(groupKey), ex8.Message);
        }

        [Fact]
        public void RegisterHystrixComman_ThrowsIfConfigNull()
        {
            ContainerBuilder services = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();
            string stringKey = "DummyCommand";

            var ex = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<DummyCommand>(services, groupKey, null));
            Assert.Contains(nameof(config), ex.Message);
            var ex2 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<IDummyCommand, DummyCommand>(services, groupKey, null));
            Assert.Contains(nameof(config), ex2.Message);
            var ex3 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<DummyCommand>(services, stringKey, null));
            Assert.Contains(nameof(config), ex3.Message);
            var ex4 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<IDummyCommand, DummyCommand>(services, stringKey, null));
            Assert.Contains(nameof(config), ex4.Message);
            var ex5 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<DummyCommand>(services, groupKey, commandKey, null));
            Assert.Contains(nameof(config), ex5.Message);
            var ex6 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<IDummyCommand, DummyCommand>(services, groupKey, commandKey, null));
            Assert.Contains(nameof(config), ex6.Message);
            var ex7 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<DummyCommand>(services, stringKey, stringKey, null));
            Assert.Contains(nameof(config), ex7.Message);
            var ex8 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<IDummyCommand, DummyCommand>(services, stringKey, stringKey, null));
            Assert.Contains(nameof(config), ex8.Message);
        }

        [Fact]
        public void RegisterHystrixCommand_ThrowsIfCommandKeyNull()
        {
            ContainerBuilder services = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();
            string stringKey = "DummyCommand";

            var ex5 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<DummyCommand>(services, groupKey, null, null));
            Assert.Contains(nameof(commandKey), ex5.Message);
            var ex6 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<IDummyCommand, DummyCommand>(services, groupKey, null, null));
            Assert.Contains(nameof(commandKey), ex6.Message);
            var ex7 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<DummyCommand>(services, stringKey, null, null));
            Assert.Contains(nameof(commandKey), ex7.Message);
            var ex8 = Assert.Throws<ArgumentNullException>(() => HystrixContainerBuilderExtensions.RegisterHystrixCommand<IDummyCommand, DummyCommand>(services, stringKey, null, null));
            Assert.Contains(nameof(commandKey), ex8.Message);
        }

        [Fact]
        public void RegisterHystrixCommand_AddsToContainer()
        {
            ContainerBuilder services = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();
            HystrixContainerBuilderExtensions.RegisterHystrixCommand<DummyCommand>(services, groupKey, config);
            var provider = services.Build();
            var command = provider.Resolve<DummyCommand>();
            Assert.NotNull(command);
            Assert.Equal(groupKey, command.CommandGroup);
            var expectedCommandKey = HystrixCommandKeyDefault.AsKey(typeof(DummyCommand).Name);
            Assert.Equal(expectedCommandKey, command.CommandKey);
            Assert.NotNull(command.Options);
            Assert.NotNull(command.Options._dynamic);

            services = new ContainerBuilder();
            config = new ConfigurationBuilder().Build();
            HystrixContainerBuilderExtensions.RegisterHystrixCommand<IDummyCommand, DummyCommand>(services, groupKey, config);
            provider = services.Build();
            var icommand = provider.Resolve<IDummyCommand>();
            Assert.NotNull(icommand);
            command = icommand as DummyCommand;
            Assert.NotNull(command);
            Assert.Equal(groupKey, command.CommandGroup);
            Assert.Equal(expectedCommandKey, command.CommandKey);
            Assert.NotNull(command.Options);
            Assert.NotNull(command.Options._dynamic);

            services = new ContainerBuilder();
            HystrixContainerBuilderExtensions.RegisterHystrixCommand<DummyCommand>(services, "GroupKey", config);
            provider = services.Build();
            command = provider.Resolve<DummyCommand>();
            Assert.NotNull(command);
            Assert.Equal("GroupKey", command.CommandGroup.Name);
            expectedCommandKey = HystrixCommandKeyDefault.AsKey(typeof(DummyCommand).Name);
            Assert.Equal(expectedCommandKey, command.CommandKey);
            Assert.NotNull(command.Options);
            Assert.NotNull(command.Options._dynamic);

            services = new ContainerBuilder();
            config = new ConfigurationBuilder().Build();
            HystrixContainerBuilderExtensions.RegisterHystrixCommand<IDummyCommand, DummyCommand>(services, "GroupKey", config);
            provider = services.Build();
            icommand = provider.Resolve<IDummyCommand>();
            Assert.NotNull(icommand);
            command = icommand as DummyCommand;
            Assert.NotNull(command);
            Assert.Equal("GroupKey", command.CommandGroup.Name);
            Assert.Equal(expectedCommandKey, command.CommandKey);
            Assert.NotNull(command.Options);
            Assert.NotNull(command.Options._dynamic);

            services = new ContainerBuilder();
            config = new ConfigurationBuilder().Build();
            HystrixContainerBuilderExtensions.RegisterHystrixCommand<DummyCommand>(services, groupKey, commandKey, config);
            provider = services.Build();
            command = provider.Resolve<DummyCommand>();
            Assert.NotNull(command);
            Assert.Equal(groupKey, command.CommandGroup);
            Assert.Equal(commandKey, command.CommandKey);
            Assert.NotNull(command.Options);
            Assert.NotNull(command.Options._dynamic);

            services = new ContainerBuilder();
            config = new ConfigurationBuilder().Build();
            HystrixContainerBuilderExtensions.RegisterHystrixCommand<IDummyCommand, DummyCommand>(services, groupKey, commandKey, config);
            provider = services.Build();
            icommand = provider.Resolve<IDummyCommand>();
            Assert.NotNull(icommand);
            command = icommand as DummyCommand;
            Assert.NotNull(command);
            Assert.Equal(groupKey, command.CommandGroup);
            Assert.Equal(commandKey, command.CommandKey);
            Assert.NotNull(command.Options);
            Assert.NotNull(command.Options._dynamic);

            services = new ContainerBuilder();
            HystrixContainerBuilderExtensions.RegisterHystrixCommand<DummyCommand>(services, "GroupKey", "CommandKey", config);
            provider = services.Build();
            command = provider.Resolve<DummyCommand>();
            Assert.NotNull(command);
            Assert.Equal("GroupKey", command.CommandGroup.Name);
            Assert.Equal("CommandKey", command.CommandKey.Name);
            Assert.NotNull(command.Options);
            Assert.NotNull(command.Options._dynamic);

            services = new ContainerBuilder();
            config = new ConfigurationBuilder().Build();
            HystrixContainerBuilderExtensions.RegisterHystrixCommand<IDummyCommand, DummyCommand>(services, "GroupKey", "CommandKey", config);
            provider = services.Build();
            icommand = provider.Resolve<IDummyCommand>();
            Assert.NotNull(icommand);
            command = icommand as DummyCommand;
            Assert.NotNull(command);
            Assert.Equal("GroupKey", command.CommandGroup.Name);
            Assert.Equal("CommandKey", command.CommandKey.Name);
            Assert.NotNull(command.Options);
            Assert.NotNull(command.Options._dynamic);
        }
    }
}
