// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Configuration;
using Steeltoe.Common.TestResources.IO;

namespace Steeltoe.Common.Test.Configuration;

public sealed class PropertyPlaceholderHelperTest
{
    [Fact]
    public void ResolvePlaceholders_ResolvesSinglePlaceholder()
    {
        const string text = "foo=${foo}";

        var appSettings = new Dictionary<string, string?>
        {
            { "foo", "bar" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result = helper.ResolvePlaceholders(text, configuration);
        Assert.Equal("foo=bar", result);
    }

    [Fact]
    public void ResolvePlaceholders_ResolvesSingleSpringPlaceholder()
    {
        const string text = "foo=${foo.bar}";

        var appSettings = new Dictionary<string, string?>
        {
            { "foo:bar", "bar" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result = helper.ResolvePlaceholders(text, configuration);
        Assert.Equal("foo=bar", result);
    }

    [Fact]
    public void ResolvePlaceholders_ResolvesMultiplePlaceholders()
    {
        const string text = "foo=${foo},bar=${bar}";

        var appSettings = new Dictionary<string, string?>
        {
            { "foo", "bar" },
            { "bar", "baz" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result = helper.ResolvePlaceholders(text, configuration);
        Assert.Equal("foo=bar,bar=baz", result);
    }

    [Fact]
    public void ResolvePlaceholders_ResolvesMultipleSpringPlaceholders()
    {
        const string text = "foo=${foo.boo},bar=${bar.far}";

        var appSettings = new Dictionary<string, string?>
        {
            { "foo:boo", "bar" },
            { "bar:far", "baz" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result = helper.ResolvePlaceholders(text, configuration);
        Assert.Equal("foo=bar,bar=baz", result);
    }

    [Fact]
    public void ResolvePlaceholders_ResolvesMultipleRecursivePlaceholders()
    {
        const string text = "foo=${bar}";

        var appSettings = new Dictionary<string, string?>
        {
            { "bar", "${baz}" },
            { "baz", "bar" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result = helper.ResolvePlaceholders(text, configuration);
        Assert.Equal("foo=bar", result);
    }

    [Fact]
    public void ResolvePlaceholders_ResolvesMultipleRecursiveSpringPlaceholders()
    {
        const string text = "foo=${bar.boo}";

        var appSettings = new Dictionary<string, string?>
        {
            { "bar:boo", "${baz.faz}" },
            { "baz:faz", "bar" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result = helper.ResolvePlaceholders(text, configuration);
        Assert.Equal("foo=bar", result);
    }

    [Fact]
    public void ResolvePlaceholders_ResolvesMultipleRecursiveInPlaceholders()
    {
        const string text1 = "foo=${b${inner}}";

        var appSettings1 = new Dictionary<string, string?>
        {
            { "bar", "bar" },
            { "inner", "ar" }
        };

        var builder1 = new ConfigurationBuilder();
        builder1.AddInMemoryCollection(appSettings1);
        IConfiguration configuration1 = builder1.Build();

        const string text2 = "${top}";

        var appSettings2 = new Dictionary<string, string?>
        {
            { "top", "${child}+${child}" },
            { "child", "${${differentiator}.grandchild}" },
            { "differentiator", "first" },
            { "first.grandchild", "actualValue" }
        };

        var builder2 = new ConfigurationBuilder();
        builder2.AddInMemoryCollection(appSettings2);
        IConfiguration configuration2 = builder2.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result1 = helper.ResolvePlaceholders(text1, configuration1);
        Assert.Equal("foo=bar", result1);

        string? result2 = helper.ResolvePlaceholders(text2, configuration2);
        Assert.Equal("actualValue+actualValue", result2);
    }

    [Fact]
    public void ResolvePlaceholders_ResolvesMultipleRecursiveInSpringPlaceholders()
    {
        const string text1 = "foo=${b${inner.placeholder}}";

        var appSettings1 = new Dictionary<string, string?>
        {
            { "bar", "bar" },
            { "inner:placeholder", "ar" }
        };

        var builder1 = new ConfigurationBuilder();
        builder1.AddInMemoryCollection(appSettings1);
        IConfiguration configuration1 = builder1.Build();

        const string text2 = "${top}";

        var appSettings2 = new Dictionary<string, string?>
        {
            { "top", "${child}+${child}" },
            { "child", "${${differentiator}.grandchild}" },
            { "differentiator", "first" },
            { "first:grandchild", "actualValue" }
        };

        var builder2 = new ConfigurationBuilder();
        builder2.AddInMemoryCollection(appSettings2);
        IConfiguration configuration2 = builder2.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result1 = helper.ResolvePlaceholders(text1, configuration1);
        Assert.Equal("foo=bar", result1);

        string? result2 = helper.ResolvePlaceholders(text2, configuration2);
        Assert.Equal("actualValue+actualValue", result2);
    }

    [Fact]
    public void ResolvePlaceholders_UnresolvedPlaceholderIsIgnored()
    {
        const string text = "foo=${foo},bar=${bar}";

        var appSettings = new Dictionary<string, string?>
        {
            { "foo", "bar" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result = helper.ResolvePlaceholders(text, configuration);
        Assert.Equal("foo=bar,bar=${bar}", result);
    }

    [Fact]
    public void ResolvePlaceholders_ResolvesArrayRefPlaceholder()
    {
        const string json = """
            {
              "vcap": {
                "application": {
                  "application_id": "fa05c1a9-0fc1-4fbd-bae1-139850dec7a3",
                  "application_name": "my-app",
                  "application_uris": [
                    "my-app.10.244.0.34.xip.io"
                  ],
                  "application_version": "fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca",
                  "limits": {
                    "disk": 1024,
                    "fds": 16384,
                    "mem": 256
                  },
                  "name": "my-app",
                  "space_id": "06450c72-4669-4dc6-8096-45f9777db68a",
                  "space_name": "my-space",
                  "uris": [
                    "my-app.10.244.0.34.xip.io",
                    "my-app2.10.244.0.34.xip.io"
                  ],
                  "users": null,
                  "version": "fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca"
                }
              }
            }
            """;

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("json", json);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);
        var builder = new ConfigurationBuilder();
        builder.SetBasePath(directory);

        builder.AddJsonFile(fileName);
        IConfiguration configuration = builder.Build();

        const string text = "foo=${vcap:application:uris[1]}";

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        string? result = helper.ResolvePlaceholders(text, configuration);
        Assert.Equal("foo=my-app2.10.244.0.34.xip.io", result);
    }

    [Fact]
    public void GetResolvedConfigurationPlaceholders_ReturnsValues_WhenResolved()
    {
        var builder = new ConfigurationBuilder();

        var appSettings = new Dictionary<string, string?>
        {
            { "foo", "${bar}" },
            { "bar", "baz" }
        };

        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        IDictionary<string, string?> resolved = helper.GetResolvedConfigurationPlaceholders(configuration);

        Assert.Contains(resolved, pair => pair.Key == "foo");
        Assert.DoesNotContain(resolved, pair => pair.Key == "bar");
        Assert.Equal("baz", resolved.First(pair => pair.Key == "foo").Value);
    }

    [Fact]
    public void GetResolvedConfigurationPlaceholders_ReturnsEmpty_WhenUnResolved()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "foo", "${bar}" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var helper = new PropertyPlaceholderHelper(NullLogger<PropertyPlaceholderHelper>.Instance);

        IDictionary<string, string?> resolved = helper.GetResolvedConfigurationPlaceholders(configuration);

        Assert.Contains(resolved, pair => pair.Key == "foo");
        Assert.Equal(string.Empty, resolved.First(k => k.Key == "foo").Value);
    }
}
