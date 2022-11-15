// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.EntityFrameworkCore.Oracle;
using Xunit;

namespace Steeltoe.Connector.EntityFrameworkCore.Test;

public class OracleDbContextOptionsExtensionsTest
{
    [Fact]
    public void UseOracle_ThrowsIfDbContextOptionsBuilderNull()
    {
        const DbContextOptionsBuilder optionsBuilder = null;
        const DbContextOptionsBuilder<GoodDbContext> goodBuilder = null;
        const IConfigurationRoot configurationRoot = null;

        var ex = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseOracle(configurationRoot));
        Assert.Contains(nameof(optionsBuilder), ex.Message, StringComparison.Ordinal);

        var ex2 = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseOracle(configurationRoot, "foobar"));
        Assert.Contains(nameof(optionsBuilder), ex2.Message, StringComparison.Ordinal);

        var ex3 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseOracle(configurationRoot));
        Assert.Contains(nameof(optionsBuilder), ex3.Message, StringComparison.Ordinal);

        var ex4 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseOracle(configurationRoot, "foobar"));
        Assert.Contains(nameof(optionsBuilder), ex4.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UseOracle_ThrowsIfConfigurationNull()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        var goodBuilder = new DbContextOptionsBuilder<GoodDbContext>();
        const IConfigurationRoot configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseOracle(configuration));
        Assert.Contains(nameof(configuration), ex.Message, StringComparison.Ordinal);

        var ex2 = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseOracle(configuration, "foobar"));
        Assert.Contains(nameof(configuration), ex2.Message, StringComparison.Ordinal);

        var ex3 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseOracle(configuration));
        Assert.Contains(nameof(configuration), ex3.Message, StringComparison.Ordinal);

        var ex4 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseOracle(configuration, "foobar"));
        Assert.Contains(nameof(configuration), ex4.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UseOracle_ThrowsIfServiceNameNull()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        var goodBuilder = new DbContextOptionsBuilder<GoodDbContext>();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        const string serviceName = null;

        var ex2 = Assert.Throws<ArgumentNullException>(() => optionsBuilder.UseOracle(configurationRoot, serviceName));
        Assert.Contains(nameof(serviceName), ex2.Message, StringComparison.Ordinal);

        var ex4 = Assert.Throws<ArgumentNullException>(() => goodBuilder.UseOracle(configurationRoot, serviceName));
        Assert.Contains(nameof(serviceName), ex4.Message, StringComparison.Ordinal);
    }
}
