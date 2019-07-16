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

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.Connector.EFCore.Test;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Oracle.EFCore.Test
{
    public class OracleDbContextOptionsExtensionsTest
    {
        [Fact]
        public void UseOracle_ThrowsIfDbContextOptionsBuilderNull()
        {
            // Arrange
            DbContextOptionsBuilder optionsBuilder = null;
            DbContextOptionsBuilder<GoodDbContext> goodBuilder = null;
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => OracleDbContextOptionsExtensions.UseOracle(optionsBuilder, config));
            Assert.Contains(nameof(optionsBuilder), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => OracleDbContextOptionsExtensions.UseOracle(optionsBuilder, config, "foobar"));
            Assert.Contains(nameof(optionsBuilder), ex2.Message);

            var ex3 = Assert.Throws<ArgumentNullException>(() => OracleDbContextOptionsExtensions.UseOracle<GoodDbContext>(goodBuilder, config));
            Assert.Contains(nameof(optionsBuilder), ex3.Message);

            var ex4 = Assert.Throws<ArgumentNullException>(() => OracleDbContextOptionsExtensions.UseOracle<GoodDbContext>(goodBuilder, config, "foobar"));
            Assert.Contains(nameof(optionsBuilder), ex4.Message);
        }

        [Fact]
        public void UseOracle_ThrowsIfConfigurationNull()
        {
            // Arrange
            DbContextOptionsBuilder optionsBuilder = new DbContextOptionsBuilder();
            DbContextOptionsBuilder<GoodDbContext> goodBuilder = new DbContextOptionsBuilder<GoodDbContext>();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => OracleDbContextOptionsExtensions.UseOracle(optionsBuilder, config));
            Assert.Contains(nameof(config), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => OracleDbContextOptionsExtensions.UseOracle(optionsBuilder, config, "foobar"));
            Assert.Contains(nameof(config), ex2.Message);

            var ex3 = Assert.Throws<ArgumentNullException>(() => OracleDbContextOptionsExtensions.UseOracle<GoodDbContext>(goodBuilder, config));
            Assert.Contains(nameof(config), ex3.Message);

            var ex4 = Assert.Throws<ArgumentNullException>(() => OracleDbContextOptionsExtensions.UseOracle<GoodDbContext>(goodBuilder, config, "foobar"));
            Assert.Contains(nameof(config), ex4.Message);
        }

        [Fact]
        public void UseOracle_ThrowsIfServiceNameNull()
        {
            // Arrange
            DbContextOptionsBuilder optionsBuilder = new DbContextOptionsBuilder();
            DbContextOptionsBuilder<GoodDbContext> goodBuilder = new DbContextOptionsBuilder<GoodDbContext>();
            IConfigurationRoot config = new ConfigurationBuilder().Build();
            string serviceName = null;

            // Act and Assert
            var ex2 = Assert.Throws<ArgumentException>(() => OracleDbContextOptionsExtensions.UseOracle(optionsBuilder, config, serviceName));
            Assert.Contains(nameof(serviceName), ex2.Message);

            var ex4 = Assert.Throws<ArgumentException>(() => OracleDbContextOptionsExtensions.UseOracle<GoodDbContext>(goodBuilder, config, serviceName));
            Assert.Contains(nameof(serviceName), ex4.Message);
        }
    }
}
