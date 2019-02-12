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
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Steeltoe.Extensions.Configuration.RandomNumber.Test
{
    public class RandomNumberSourceTest
    {

        [Fact]
        public void Constructors__InitializesDefaults()
        {

            ILoggerFactory factory = new LoggerFactory();

            var source = new RandomNumberSource(factory);
            Assert.Equal(factory, source._loggerFactory);
            Assert.NotNull(source._prefix);
            Assert.Equal(RandomNumberSource.PREFIX, source._prefix);

            source = new RandomNumberSource("foobar:", factory);
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
            var source = new RandomNumberSource();
            var provider = source.Build(new ConfigurationBuilder());
            Assert.IsType<RandomNumberProvider>(provider);
        }
    }
}
