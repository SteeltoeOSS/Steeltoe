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
        var appSettings = new Dictionary<string, string?>
        {
            ["foo:bar"] = "value"
        };

        IConfigurationBuilder builder = new ConfigurationBuilder().AddRandomValueSource().AddInMemoryCollection(appSettings);
        IConfigurationRoot configurationRoot = builder.Build();
        string? value = configurationRoot["foo:bar"];

        value.Should().Be("value");
    }

    [Fact]
    public void AddRandomValueSource_String()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddRandomValueSource();
        IConfigurationRoot configurationRoot = builder.Build();
        string? value = configurationRoot["random:string"];

        value.Should().NotBeNull();
    }

    [Fact]
    public void AddRandomValueSource_Uuid()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddRandomValueSource();
        IConfigurationRoot configurationRoot = builder.Build();
        string? value = configurationRoot["random:uuid"];

        value.Should().NotBeNull();
    }

    [Fact]
    public void AddRandomValueSource_RandomInt()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddRandomValueSource();
        IConfigurationRoot configurationRoot = builder.Build();
        string? value = configurationRoot["random:int"];

        value.Should().NotBeNull();
    }

    [Fact]
    public void AddRandomValueSource_RandomIntRange()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddRandomValueSource();
        IConfigurationRoot configurationRoot = builder.Build();
        string? value = configurationRoot["random:int[4,10]"];

        value.Should().NotBeNull();

        int number = int.Parse(value, CultureInfo.InvariantCulture);
        number.Should().BeInRange(4, 10);
    }

    [Fact]
    public void AddRandomValueSource_RandomIntMax()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddRandomValueSource();
        IConfigurationRoot configurationRoot = builder.Build();
        string? value = configurationRoot["random:int(10)"];

        value.Should().NotBeNull();

        int number = int.Parse(value, CultureInfo.InvariantCulture);

        number.Should().BeInRange(0, 10);
    }

    [Fact]
    public void AddRandomValueSource_RandomLong()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddRandomValueSource();
        IConfigurationRoot configurationRoot = builder.Build();
        string? value = configurationRoot["random:long"];

        value.Should().NotBeNull();
    }

    [Fact]
    public void AddRandomValueSource_RandomLongRange()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddRandomValueSource();
        IConfigurationRoot configurationRoot = builder.Build();
        string? value = configurationRoot["random:long[4,10]"];

        value.Should().NotBeNull();

        int number = int.Parse(value, CultureInfo.InvariantCulture);

        number.Should().BeInRange(4, 10);
    }

    [Fact]
    public void AddRandomValueSource_RandomLongMax()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddRandomValueSource();
        IConfigurationRoot configurationRoot = builder.Build();
        string? value = configurationRoot["random:long(10)"];

        value.Should().NotBeNull();

        int number = int.Parse(value, CultureInfo.InvariantCulture);

        number.Should().BeInRange(0, 10);
    }
}
