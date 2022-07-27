// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Common.Test.Options;

public class AbstractOptionsTest
{
    [Fact]
    public void Constructors_ThrowsOnNulls()
    {
        IConfigurationRoot root = null;
        IConfiguration config = null;

        Assert.Throws<ArgumentNullException>(() => new TestOptions(root, "foobar"));
        Assert.Throws<ArgumentNullException>(() => new TestOptions(config));
    }

    [Fact]
    public void Constructors_BindsValues()
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(new Dictionary<string, string>()
        {
            { "foo", "bar" }
        });

        var root = builder.Build();
        IConfiguration config = root;

        var opt1 = new TestOptions(root);
        Assert.Equal("bar", opt1.Foo);

        var opt2 = new TestOptions(config);
        Assert.Equal("bar", opt2.Foo);

        var builder2 = new ConfigurationBuilder();
        builder2.AddInMemoryCollection(new Dictionary<string, string>()
        {
            { "prefix:foo", "bar" }
        });
        var root2 = builder2.Build();
        var opt3 = new TestOptions(root2, "prefix");
        Assert.Equal("bar", opt3.Foo);
    }
}