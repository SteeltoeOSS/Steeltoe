// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Common.Test.Options;

public sealed class AbstractOptionsTest
{
    [Fact]
    public void Constructor_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => new TestOptions(null, "foobar"));
    }

    [Fact]
    public void Constructors_BindsValues()
    {
        var builder1 = new ConfigurationBuilder();

        builder1.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "foo", "bar" }
        });

        IConfigurationRoot root = builder1.Build();
        IConfiguration configuration = root;

        var options1 = new TestOptions(root, null);
        Assert.Equal("bar", options1.Foo);

        var options2 = new TestOptions(configuration, null);
        Assert.Equal("bar", options2.Foo);

        var builder2 = new ConfigurationBuilder();

        builder2.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "prefix:foo", "bar" }
        });

        IConfigurationRoot root2 = builder2.Build();
        var options3 = new TestOptions(root2, "prefix");
        Assert.Equal("bar", options3.Foo);
    }
}
