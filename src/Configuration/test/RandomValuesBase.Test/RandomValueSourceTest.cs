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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Steeltoe.Extensions.Configuration.RandomValue.Test
{
    public class RandomValueSourceTest
    {
        [Fact]
        public void Constructors__InitializesDefaults()
        {
            ILoggerFactory factory = new LoggerFactory();

            var source = new RandomValueSource(factory);
            Assert.Equal(factory, source._loggerFactory);
            Assert.NotNull(source._prefix);
            Assert.Equal(RandomValueSource.PREFIX, source._prefix);

            source = new RandomValueSource("foobar:", factory);
            Assert.Equal(factory, source._loggerFactory);
            Assert.NotNull(source._prefix);
            Assert.Equal("foobar:", source._prefix);
        }

        [Fact]
        public void Build__ReturnsProvider()
        {
            // Arrange
            ILoggerFactory factory = new LoggerFactory();

            // Act and Assert
            var source = new RandomValueSource();
            var provider = source.Build(new ConfigurationBuilder());
            Assert.IsType<RandomValueProvider>(provider);
        }
    }
}
