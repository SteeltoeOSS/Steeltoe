// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.EntityFrameworkCore.Test;
using Xunit;

namespace Steeltoe.Connector.Oracle.EntityFrameworkCore.Test;

public class OracleDbContextOptionsExtensionsTest
{
    [Fact]
    public void UseOracle_ThrowsIfDbContextOptionsBuilderNull()
    {
        const DbContextOptionsBuilder optionsBuilder = null;
        const DbContextOptionsBuilder<GoodDbContext> goodBuilder = null;
        const IConfigurationRoot config = null;

        var ex = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseOracle(config));
        Assert.Contains(nameof(optionsBuilder), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseOracle(config, "foobar"));
        Assert.Contains(nameof(optionsBuilder), ex2.Message);

        var ex3 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseOracle(config));
        Assert.Contains(nameof(optionsBuilder), ex3.Message);

        var ex4 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseOracle(config, "foobar"));
        Assert.Contains(nameof(optionsBuilder), ex4.Message);
    }

    [Fact]
    public void UseOracle_ThrowsIfConfigurationNull()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        var goodBuilder = new DbContextOptionsBuilder<GoodDbContext>();
        const IConfigurationRoot config = null;

        var ex = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseOracle(config));
        Assert.Contains(nameof(config), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseOracle(config, "foobar"));
        Assert.Contains(nameof(config), ex2.Message);

        var ex3 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseOracle(config));
        Assert.Contains(nameof(config), ex3.Message);

        var ex4 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseOracle(config, "foobar"));
        Assert.Contains(nameof(config), ex4.Message);
    }

    [Fact]
    public void UseOracle_ThrowsIfServiceNameNull()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        var goodBuilder = new DbContextOptionsBuilder<GoodDbContext>();
        IConfigurationRoot config = new ConfigurationBuilder().Build();
        const string serviceName = null;

        var ex2 = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseOracle(config, serviceName));
        Assert.Contains(nameof(serviceName), ex2.Message);

        var ex4 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseOracle(config, serviceName));
        Assert.Contains(nameof(serviceName), ex4.Message);
    }
}
