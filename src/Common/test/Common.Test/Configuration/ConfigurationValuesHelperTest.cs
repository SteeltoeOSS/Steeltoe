// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Configuration;

namespace Steeltoe.Common.Test.Configuration;

public sealed class ConfigurationValuesHelperTest
{
    [Fact]
    public void GetString_NoResolveFromConfig()
    {
        var settings = new Dictionary<string, string?>
        {
            { "a:b", "astring" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        string? result = ConfigurationValuesHelper.GetString("a:b", configuration, null, null);
        Assert.Equal("astring", result);
    }

    [Fact]
    public void GetInt32_ReturnsValue()
    {
        var settings = new Dictionary<string, string?>
        {
            { "a:b", "100" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        int result = ConfigurationValuesHelper.GetInt32("a:b", configuration, null, 500);
        Assert.Equal(100, result);
    }

    [Fact]
    public void GetDouble_ReturnsValue()
    {
        var settings = new Dictionary<string, string?>
        {
            { "a:b", "100.00" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        double result = ConfigurationValuesHelper.GetDouble("a:b", configuration, null, 500.00);
        Assert.Equal(100.00, result);
    }

    [Fact]
    public void GetBoolean_ReturnsValue()
    {
        var settings = new Dictionary<string, string?>
        {
            { "a:b", "True" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        bool result = ConfigurationValuesHelper.GetBoolean("a:b", configuration, null, false);
        Assert.True(result);
    }

    [Fact]
    public void GetInt_NotFoundReturnsDefault()
    {
        var settings = new Dictionary<string, string?>
        {
            { "a:b", "astring" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        int result = ConfigurationValuesHelper.GetInt32("a:b:c", configuration, null, 100);
        Assert.Equal(100, result);
    }

    [Fact]
    public void GetDouble_NotFoundReturnsDefault()
    {
        var settings = new Dictionary<string, string?>
        {
            { "a:b", "astring" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        double result = ConfigurationValuesHelper.GetDouble("a:b:c", configuration, null, 100.00);
        Assert.Equal(100.00, result);
    }

    [Fact]
    public void GetBoolean_NotFoundReturnsDefault()
    {
        var settings = new Dictionary<string, string?>
        {
            { "a:b", "astring" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        bool result = ConfigurationValuesHelper.GetBoolean("a:b:c", configuration, null, true);
        Assert.True(result);
    }

    [Fact]
    public void GetString_NotFoundReturnsDefault()
    {
        var settings = new Dictionary<string, string?>
        {
            { "a:b", "astring" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        string? result = ConfigurationValuesHelper.GetString("a:b:c", configuration, null, "foobar");
        Assert.Equal("foobar", result);
    }

    [Fact]
    public void GetString_ResolvesReference()
    {
        var settings1 = new Dictionary<string, string?>
        {
            { "a:b", "${a:b:c}" }
        };

        var settings2 = new Dictionary<string, string?>
        {
            { "a:b:c", "astring" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings1).Build();
        IConfiguration resolve = new ConfigurationBuilder().AddInMemoryCollection(settings2).Build();

        string? result = ConfigurationValuesHelper.GetString("a:b", configuration, resolve, "foobar");
        Assert.Equal("astring", result);
    }

    [Fact]
    public void GetString_ResolveNotFoundReturnsNotResolvedValue()
    {
        var settings1 = new Dictionary<string, string?>
        {
            { "a:b", "${a:b:c}" }
        };

        var settings2 = new Dictionary<string, string?>
        {
            { "a:b:d", "astring" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings1).Build();
        IConfiguration resolve = new ConfigurationBuilder().AddInMemoryCollection(settings2).Build();

        string? result = ConfigurationValuesHelper.GetString("a:b", configuration, resolve, null);
        Assert.Equal("${a:b:c}", result);
    }

    [Fact]
    public void GetString_ResolveNotFoundReturnsPlaceholderDefault()
    {
        var settings1 = new Dictionary<string, string?>
        {
            { "a:b", "${a:b:c?placeholderdefault}" }
        };

        var settings2 = new Dictionary<string, string?>
        {
            { "a:b:d", "astring" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings1).Build();
        IConfiguration resolve = new ConfigurationBuilder().AddInMemoryCollection(settings2).Build();

        string? result = ConfigurationValuesHelper.GetString("a:b", configuration, resolve, "foobar");
        Assert.Equal("placeholderdefault", result);
    }

    [Fact]
    public void GetSetting_GetsFromFirst()
    {
        var settings1 = new Dictionary<string, string?>
        {
            { "a:b", "setting1" }
        };

        var settings2 = new Dictionary<string, string?>
        {
            { "a:b", "setting2" }
        };

        IConfiguration config1 = new ConfigurationBuilder().AddInMemoryCollection(settings1).Build();
        IConfiguration config2 = new ConfigurationBuilder().AddInMemoryCollection(settings2).Build();

        string? result = ConfigurationValuesHelper.GetSetting("a:b", config1, config2, null, "foobar");
        Assert.Equal("setting1", result);
    }

    [Fact]
    public void GetSetting_GetsFromSecond()
    {
        var settings1 = new Dictionary<string, string?>
        {
            { "a:b:c", "setting1" }
        };

        var settings2 = new Dictionary<string, string?>
        {
            { "a:b", "setting2" }
        };

        IConfiguration config1 = new ConfigurationBuilder().AddInMemoryCollection(settings1).Build();
        IConfiguration config2 = new ConfigurationBuilder().AddInMemoryCollection(settings2).Build();

        string? result = ConfigurationValuesHelper.GetSetting("a:b", config1, config2, null, "foobar");
        Assert.Equal("setting2", result);
    }
}
