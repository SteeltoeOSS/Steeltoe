// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Steeltoe.Configuration.Placeholder.Test;

public sealed class PropertyPlaceholderHelperTest
{
    [Fact]
    public void ResolvePlaceholders_ResolvesSinglePlaceholder()
    {
        const string text = "foo=${foo}";

        var appSettings = new Dictionary<string, string?>
        {
            ["foo"] = "bar"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result = helper.ResolvePlaceholders(text, configuration);

        result.Should().Be("foo=bar");
    }

    [Fact]
    public void ResolvePlaceholders_ResolvesSinglePlaceholder_WithDefault()
    {
        const string text = "foo=${foo?empty}";

        var appSettings = new Dictionary<string, string?>
        {
            ["foo"] = "bar"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result = helper.ResolvePlaceholders(text, configuration);

        result.Should().Be("foo=bar");
    }

    [Fact]
    public void ResolvePlaceholders_ResolvesSinglePlaceholder_UsesDefault()
    {
        const string text = "foo=${foo?empty}";

        var builder = new ConfigurationBuilder();
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result = helper.ResolvePlaceholders(text, configuration);

        result.Should().Be("foo=empty");
    }

    [Fact]
    public void ResolvePlaceholders_ResolvesSinglePlaceholder_ResolvesPlaceholderInDefault()
    {
        const string text = "foo=${foo?${myDefault}}";

        var appSettings = new Dictionary<string, string?>
        {
            ["myDefault"] = "empty"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result = helper.ResolvePlaceholders(text, configuration);

        result.Should().Be("foo=empty");
    }

    [Fact]
    public void ResolvePlaceholders_ResolvesSingleSpringPlaceholder()
    {
        const string text = "foo=${foo.bar}";

        var appSettings = new Dictionary<string, string?>
        {
            ["foo:bar"] = "bar"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result = helper.ResolvePlaceholders(text, configuration);

        result.Should().Be("foo=bar");
    }

    [Fact]
    public void ResolvePlaceholders_ResolvesSingleSpringPlaceholder_WithDefault()
    {
        const string text = "foo=${foo.bar?empty}";

        var appSettings = new Dictionary<string, string?>
        {
            ["foo:bar"] = "bar"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result = helper.ResolvePlaceholders(text, configuration);

        result.Should().Be("foo=bar");
    }

    [Fact]
    public void ResolvePlaceholders_ResolvesSingleSpringPlaceholder_UsesDefault()
    {
        const string text = "foo=${foo.bar?empty}";

        var builder = new ConfigurationBuilder();
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result = helper.ResolvePlaceholders(text, configuration);

        result.Should().Be("foo=empty");
    }

    [Fact]
    public void ResolvePlaceholders_ResolvesMultiplePlaceholders()
    {
        const string text = "foo=${foo},bar=${bar}";

        var appSettings = new Dictionary<string, string?>
        {
            ["foo"] = "bar",
            ["bar"] = "baz"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result = helper.ResolvePlaceholders(text, configuration);

        result.Should().Be("foo=bar,bar=baz");
    }

    [Fact]
    public void ResolvePlaceholders_ResolvesMultipleSpringPlaceholders()
    {
        const string text = "foo=${foo.boo},bar=${bar.far}";

        var appSettings = new Dictionary<string, string?>
        {
            ["foo:boo"] = "bar",
            ["bar:far"] = "baz"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result = helper.ResolvePlaceholders(text, configuration);

        result.Should().Be("foo=bar,bar=baz");
    }

    [Fact]
    public void ResolvePlaceholders_ResolvesMultipleRecursivePlaceholders()
    {
        const string text = "foo=${bar}";

        var appSettings = new Dictionary<string, string?>
        {
            ["bar"] = "${baz}",
            ["baz"] = "bar"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result = helper.ResolvePlaceholders(text, configuration);

        result.Should().Be("foo=bar");
    }

    [Fact]
    public void ResolvePlaceholders_ResolvesMultipleRecursiveSpringPlaceholders()
    {
        const string text = "foo=${bar.boo}";

        var appSettings = new Dictionary<string, string?>
        {
            ["bar:boo"] = "${baz.faz}",
            ["baz:faz"] = "bar"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result = helper.ResolvePlaceholders(text, configuration);

        result.Should().Be("foo=bar");
    }

    [Fact]
    public void ResolvePlaceholders_ResolvesMultipleRecursiveInPlaceholders()
    {
        const string text1 = "foo=${b${inner}}";

        var appSettings1 = new Dictionary<string, string?>
        {
            ["bar"] = "bar",
            ["inner"] = "ar"
        };

        var builder1 = new ConfigurationBuilder();
        builder1.AddInMemoryCollection(appSettings1);
        IConfiguration configuration1 = builder1.Build();

        const string text2 = "${top}";

        var appSettings2 = new Dictionary<string, string?>
        {
            ["top"] = "${child}+${child}",
            ["child"] = "${${differentiator}.grandchild}",
            ["differentiator"] = "first",
            ["first.grandchild"] = "actualValue"
        };

        var builder2 = new ConfigurationBuilder();
        builder2.AddInMemoryCollection(appSettings2);
        IConfiguration configuration2 = builder2.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result1 = helper.ResolvePlaceholders(text1, configuration1);

        result1.Should().Be("foo=bar");

        string? result2 = helper.ResolvePlaceholders(text2, configuration2);

        result2.Should().Be("actualValue+actualValue");
    }

    [Fact]
    public void ResolvePlaceholders_ResolvesMultipleRecursiveInSpringPlaceholders()
    {
        const string text1 = "foo=${b${inner.placeholder}}";

        var appSettings1 = new Dictionary<string, string?>
        {
            ["bar"] = "bar",
            ["inner:placeholder"] = "ar"
        };

        var builder1 = new ConfigurationBuilder();
        builder1.AddInMemoryCollection(appSettings1);
        IConfiguration configuration1 = builder1.Build();

        const string text2 = "${top}";

        var appSettings2 = new Dictionary<string, string?>
        {
            ["top"] = "${child}+${child}",
            ["child"] = "${${differentiator}.grandchild}",
            ["differentiator"] = "first",
            ["first:grandchild"] = "actualValue"
        };

        var builder2 = new ConfigurationBuilder();
        builder2.AddInMemoryCollection(appSettings2);
        IConfiguration configuration2 = builder2.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result1 = helper.ResolvePlaceholders(text1, configuration1);

        result1.Should().Be("foo=bar");

        string? result2 = helper.ResolvePlaceholders(text2, configuration2);

        result2.Should().Be("actualValue+actualValue");
    }

    [Fact]
    public void ResolvePlaceholders_UnresolvedPlaceholderIsIgnored()
    {
        const string text = "foo=${foo},bar=${bar}";

        var appSettings = new Dictionary<string, string?>
        {
            ["foo"] = "bar"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result = helper.ResolvePlaceholders(text, configuration);

        result.Should().Be("foo=bar,bar=${bar}");
    }

    [Fact]
    public void ResolvePlaceholders_ResolvesArrayRefPlaceholder()
    {
        const string text = "line=${root:sub:lines[2]}";

        var appSettings = new Dictionary<string, string?>
        {
            ["root:sub:lines:0"] = "zero",
            ["root:sub:lines:1"] = "one",
            ["root:sub:lines:2"] = "two"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result = helper.ResolvePlaceholders(text, configuration);

        result.Should().Be("line=two");
    }

    [Fact]
    public void ResolvePlaceholders_ResolvesArrayRefPlaceholder_WithDefault()
    {
        const string text = "line=${root:sub:lines[2]?empty}";

        var appSettings = new Dictionary<string, string?>
        {
            ["root:sub:lines:0"] = "zero",
            ["root:sub:lines:1"] = "one",
            ["root:sub:lines:2"] = "two"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result = helper.ResolvePlaceholders(text, configuration);

        result.Should().Be("line=two");
    }

    [Fact]
    public void ResolvePlaceholders_ResolvesArrayRefPlaceholder_UsesDefault()
    {
        const string text = "line=${root:sub:lines[2]?empty}";

        var builder = new ConfigurationBuilder();
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result = helper.ResolvePlaceholders(text, configuration);

        result.Should().Be("line=empty");
    }
}
