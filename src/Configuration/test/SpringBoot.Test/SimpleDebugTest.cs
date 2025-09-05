// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBeInternal
// ReSharper disable CollectionNeverUpdated.Global
#pragma warning disable CA1805 // Do not initialize unnecessarily
#pragma warning disable S3052 // Members should not be initialized to default values
#pragma warning disable S4004 // Collection properties should be readonly
#pragma warning disable IDE0028 // Simplify collection initialization
#pragma warning disable SA1005 // Single line comments should begin with single space
#pragma warning disable S3776 // Cognitive Complexity of methods should not be too high
#pragma warning disable S125 // Sections of code should not be commented out

namespace Steeltoe.Configuration.SpringBoot.Test;

public sealed class SimpleDebugTest
{
    private static readonly bool IsDotNet10 = typeof(JsonStreamConfigurationProvider).Assembly.GetName().Version?.Major == 10;
    private static readonly SimpleElement EmptyChild = new();

    [Fact]
    public void DebugMe_SetToNull()
    {
        const string json = """
            {
              "root": {
                "NullableChildWithDefaultEmpty": null,
              }
            }
            """;

        using MemoryStream stream = StringToStream(json);
        IConfigurationRoot configuration = new ConfigurationBuilder().AddCustomJsonStream(stream).Build();
        JsonStreamConfigurationProvider configurationProvider = configuration.Providers.OfType<JsonStreamConfigurationProvider>().Single();

        //var instance = new SimpleContainer();
        //IConfigurationSection section = configuration.GetSection("root");
        //CustomOptionsBuilderConfigurationExtensions.CustomBind(section, instance);

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<SimpleContainer>().CustomBindConfiguration("root");
        using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<SimpleContainer>>();
        SimpleContainer container = optionsMonitor.CurrentValue;

        using (new AssertionScope())
        {
            if (IsDotNet10)
            {
                container.NullableChildWithDefaultEmpty.Should().BeNull();
            }
            else
            {
                container.NullableChildWithDefaultEmpty.Should().BeEquivalentTo(EmptyChild);
            }

            AssertGetValue(configurationProvider, "root:NullableChildWithDefaultEmpty", IsDotNet10 ? null : string.Empty);
        }
    }

    [Fact]
    public void DebugMe_SetToNewInstance()
    {
        const string json = """
            {
              "root": {
                "NullableChild": {},
              }
            }
            """;

        using MemoryStream stream = StringToStream(json);
        IConfigurationRoot configuration = new ConfigurationBuilder().AddCustomJsonStream(stream).Build();
        JsonStreamConfigurationProvider configurationProvider = configuration.Providers.OfType<JsonStreamConfigurationProvider>().Single();

        //var instance = new SimpleContainer();
        //IConfigurationSection section = configuration.GetSection("root");
        //CustomOptionsBuilderConfigurationExtensions.CustomBind(section, instance);

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<SimpleContainer>().CustomBindConfiguration("root");
        using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<SimpleContainer>>();
        SimpleContainer container = optionsMonitor.CurrentValue;

        using (new AssertionScope())
        {
            if (IsDotNet10)
            {
                container.NullableChild.Should().BeEquivalentTo(EmptyChild);
            }
            else
            {
                container.NullableChild.Should().BeNull();
            }

            AssertGetValue(configurationProvider, "root:NullableChild", IsDotNet10 ? string.Empty : null);
        }
    }

    private static MemoryStream StringToStream(string json)
    {
        var stream = new MemoryStream();

        using (var writer = new StreamWriter(stream, leaveOpen: true))
        {
            writer.Write(json);
        }

        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    [CustomAssertion]
    private static void AssertGetValue(JsonStreamConfigurationProvider provider, string key, string? expectedValue)
    {
        provider.TryGet(key, out string? value).Should().BeTrue($"at key {key}");
        value.Should().Be(expectedValue, $"at key {key}");
    }

    public class SimpleElement
    {
        public int? NullableInt32 { get; set; }
    }

    public sealed class SimpleContainer : SimpleElement
    {
        public SimpleElement? NullableChildWithDefaultEmpty { get; set; } = new();
        public SimpleElement? NullableChild { get; set; }
    }
}
