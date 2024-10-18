// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.RandomValue.Test;

public sealed class RandomValueConfigurationBuilderExtensionsTest
{
    [Fact]
    public void AddRandomValueSource_Ignores()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddRandomValueSource().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "foo:bar", "value" }
        });

        IConfigurationRoot configurationRoot = builder.Build();
        string? value = configurationRoot["foo:bar"];
        Assert.Equal("value", value);
    }

    [Fact]
    public void AddRandomValueSource_String()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddRandomValueSource().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "foo:bar", "value" }
        });

        IConfigurationRoot configurationRoot = builder.Build();
        string? value = configurationRoot["random:string"];
        Assert.NotNull(value);
    }

    [Fact]
    public void AddRandomValueSource_Uuid()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddRandomValueSource().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "foo:bar", "value" }
        });

        IConfigurationRoot configurationRoot = builder.Build();
        string? value = configurationRoot["random:uuid"];
        Assert.NotNull(value);
    }

    [Fact]
    public void AddRandomValueSource_RandomInt()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddRandomValueSource().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "foo:bar", "value" }
        });

        IConfigurationRoot configurationRoot = builder.Build();
        string? value = configurationRoot["random:int"];
        Assert.NotNull(value);
    }

    [Fact]
    public void AddRandomValueSource_RandomIntRange()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddRandomValueSource().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "foo:bar", "value" }
        });

        IConfigurationRoot configurationRoot = builder.Build();
        string? value = configurationRoot["random:int[4,10]"];
        Assert.NotNull(value);
        int number = int.Parse(value, CultureInfo.InvariantCulture);
        Assert.InRange(number, 4, 10);
    }

    [Fact]
    public void AddRandomValueSource_RandomIntMax()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddRandomValueSource().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "foo:bar", "value" }
        });

        IConfigurationRoot configurationRoot = builder.Build();
        string? value = configurationRoot["random:int(10)"];
        Assert.NotNull(value);
        int number = int.Parse(value, CultureInfo.InvariantCulture);
        Assert.InRange(number, 0, 10);
    }

    [Fact]
    public void AddRandomValueSource_RandomLong()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddRandomValueSource().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "foo:bar", "value" }
        });

        IConfigurationRoot configurationRoot = builder.Build();
        string? value = configurationRoot["random:long"];
        Assert.NotNull(value);
    }

    [Fact]
    public void AddRandomValueSource_RandomLongRange()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddRandomValueSource().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "foo:bar", "value" }
        });

        IConfigurationRoot configurationRoot = builder.Build();
        string? value = configurationRoot["random:long[4,10]"];
        Assert.NotNull(value);
        int number = int.Parse(value, CultureInfo.InvariantCulture);
        Assert.InRange(number, 4, 10);
    }

    [Fact]
    public void AddRandomValueSource_RandomLongMax()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddRandomValueSource().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "foo:bar", "value" }
        });

        IConfigurationRoot configurationRoot = builder.Build();
        string? value = configurationRoot["random:long(10)"];
        Assert.NotNull(value);
        int number = int.Parse(value, CultureInfo.InvariantCulture);
        Assert.InRange(number, 0, 10);
    }
}
