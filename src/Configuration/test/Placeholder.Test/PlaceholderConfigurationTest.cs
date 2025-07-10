// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Configuration.Placeholder.Test;

public sealed class PlaceholderConfigurationTest : IDisposable
{
    private readonly LoggerFactory _loggerFactory;

    public PlaceholderConfigurationTest(ITestOutputHelper testOutputHelper)
    {
        var loggerProvider = new XunitLoggerProvider(testOutputHelper);
        _loggerFactory = new LoggerFactory([loggerProvider]);
    }

    [Fact]
    public void Takes_ownership_of_existing_sources()
    {
        var testSourceA = new TestConfigurationSource("A", _loggerFactory);
        var testSourceB = new TestConfigurationSource("B", _loggerFactory);

        var builder = new ConfigurationBuilder();
        builder.Add(testSourceA);
        builder.Add(testSourceB);
        builder.AddPlaceholderResolver(_loggerFactory);

        PlaceholderConfigurationSource placeholderSource =
            builder.Sources.Should().ContainSingle().Which.Should().BeOfType<PlaceholderConfigurationSource>().Subject;

        placeholderSource.Sources.Should().HaveCount(2);
        placeholderSource.Sources.Should().Contain(testSourceA);
        placeholderSource.Sources.Should().Contain(testSourceB);
    }

    [Fact]
    public void Reloads_owned_providers()
    {
        var testSourceA = new TestConfigurationSource("A", _loggerFactory);
        Guid testSourceIdA = testSourceA.Id;
        var testSourceB = new TestConfigurationSource("B", _loggerFactory);
        Guid testSourceIdB = testSourceB.Id;

        var builder = new ConfigurationBuilder();
        builder.Add(testSourceA);
        builder.Add(testSourceB);
        builder.AddPlaceholderResolver(_loggerFactory);

        testSourceA.LastProvider.Should().BeNull();
        testSourceB.LastProvider.Should().BeNull();

        IConfigurationRoot configurationRoot = builder.Build();

        testSourceA.LastProvider.Should().NotBeNull();
        testSourceB.LastProvider.Should().NotBeNull();

        testSourceA.LastProvider.LoadCount.Should().Be(1);
        testSourceB.LastProvider.LoadCount.Should().Be(1);

        Guid lastProviderIdA = testSourceA.LastProvider.Id;
        Guid lastProviderIdB = testSourceB.LastProvider.Id;

        configurationRoot.Reload();

        testSourceA.LastProvider.LoadCount.Should().Be(2);
        testSourceB.LastProvider.LoadCount.Should().Be(2);

        configurationRoot.Reload();

        testSourceA.LastProvider.LoadCount.Should().Be(3);
        testSourceB.LastProvider.LoadCount.Should().Be(3);

        testSourceA.Id.Should().Be(testSourceIdA);
        testSourceB.Id.Should().Be(testSourceIdB);

        lastProviderIdA.Should().Be(testSourceA.LastProvider.Id);
        lastProviderIdB.Should().Be(testSourceB.LastProvider.Id);
    }

    [Fact]
    public void Disposes_owned_providers()
    {
        var testSourceA = new TestConfigurationSource("A", _loggerFactory);
        var testSourceB = new TestConfigurationSource("B", _loggerFactory);

        var builder = new ConfigurationBuilder();
        builder.Add(testSourceA);
        builder.Add(testSourceB);
        builder.AddPlaceholderResolver(_loggerFactory);

        var configurationRoot = (ConfigurationRoot)builder.Build();

        testSourceA.LastProvider.Should().NotBeNull();
        testSourceB.LastProvider.Should().NotBeNull();

        testSourceA.LastProvider.DisposeCount.Should().Be(0);
        testSourceB.LastProvider.DisposeCount.Should().Be(0);

        configurationRoot.Dispose();

        testSourceA.LastProvider.DisposeCount.Should().Be(1);
        testSourceB.LastProvider.DisposeCount.Should().Be(1);

#pragma warning disable S3966 // Objects should not be disposed more than once
        configurationRoot.Dispose();
#pragma warning restore S3966 // Objects should not be disposed more than once

        testSourceA.LastProvider.DisposeCount.Should().Be(1);
        testSourceB.LastProvider.DisposeCount.Should().Be(1);
    }

    [Fact]
    public void Configuration_rebuild_creates_new_providers()
    {
        var testSourceA = new TestConfigurationSource("A", _loggerFactory);
        Guid testSourceIdA = testSourceA.Id;
        var testSourceB = new TestConfigurationSource("B", _loggerFactory);
        Guid testSourceIdB = testSourceB.Id;

        var builder = new ConfigurationBuilder();
        builder.Add(testSourceA);
        builder.Add(testSourceB);
        builder.AddPlaceholderResolver(_loggerFactory);

        TestConfigurationProvider? previousProviderA;
        TestConfigurationProvider? previousProviderB;

        using ((ConfigurationRoot)builder.Build())
        {
            previousProviderA = testSourceA.LastProvider;
            previousProviderB = testSourceA.LastProvider;
        }

        _ = builder.Build();

        TestConfigurationProvider? nextProviderA = testSourceA.LastProvider;
        TestConfigurationProvider? nextProviderB = testSourceA.LastProvider;

        previousProviderA.Should().NotBeNull();
        previousProviderB.Should().NotBeNull();

        nextProviderA.Should().NotBeNull();
        nextProviderB.Should().NotBeNull();

        previousProviderA.Id.Should().NotBe(nextProviderA.Id);
        previousProviderB.Id.Should().NotBe(nextProviderB.Id);

        previousProviderA.LoadCount.Should().Be(1);
        previousProviderB.LoadCount.Should().Be(1);

        previousProviderA.DisposeCount.Should().Be(1);
        previousProviderB.DisposeCount.Should().Be(1);

        nextProviderA.LoadCount.Should().Be(1);
        nextProviderB.LoadCount.Should().Be(1);

        nextProviderA.DisposeCount.Should().Be(0);
        nextProviderB.DisposeCount.Should().Be(0);

        testSourceA.Id.Should().Be(testSourceIdA);
        testSourceB.Id.Should().Be(testSourceIdB);
    }

    [Fact]
    public void Forwards_key_lookups_to_owned_providers()
    {
        var testSourceA = new TestConfigurationSource("A", _loggerFactory);
        var testSourceB = new TestConfigurationSource("B", _loggerFactory);

        var builder = new ConfigurationBuilder();
        builder.Add(testSourceA);
        builder.Add(testSourceB);
        builder.AddPlaceholderResolver(_loggerFactory);
        IConfigurationRoot configurationRoot = builder.Build();

        testSourceA.LastProvider.Should().NotBeNull();
        testSourceB.LastProvider.Should().NotBeNull();

        testSourceA.LastProvider.Set("testRoot:keyA", "valueA");
        testSourceB.LastProvider.Set("testRoot:keyB", "valueB");

        configurationRoot["testRoot:keyA"].Should().Be("valueA");
        configurationRoot["testRoot:keyB"].Should().Be("valueB");

        testSourceA.LastProvider.Set("testRoot:keyA", "alt-valueA");
        testSourceB.LastProvider.Set("testRoot:keyB", null);

        configurationRoot["testRoot:keyA"].Should().Be("alt-valueA");
        configurationRoot["testRoot:keyB"].Should().BeNull();
    }

    [Fact]
    public void Substitutes_placeholders_in_key_lookups()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["key1"] = "value1",
            ["key2"] = "${key1?not-found}",
            ["key3"] = "${no-key?not-found}",
            ["key4"] = "${no-key}",
            ["key5"] = string.Empty
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        builder.AddPlaceholderResolver(_loggerFactory);
        IConfiguration configuration = builder.Build();

        configuration["no-key"].Should().BeNull();
        configuration["key1"].Should().Be("value1");
        configuration["key2"].Should().Be("value1");
        configuration["key3"].Should().Be("not-found");
        configuration["key4"].Should().Be("${no-key}");
        configuration["key5"].Should().BeEmpty();

        configuration["no-key"] = "new-key-value";

        configuration["key3"].Should().Be("new-key-value");
        configuration["key4"].Should().Be("new-key-value");
    }

    [Fact]
    public void Substitutes_placeholders_in_section_lookups()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["appName"] = "test",
            ["one"] = "value1-${appName}",
            ["one:two"] = "value2-${appName}",
            ["one:two:three"] = "value3-${appName}",
            ["one:two:three:four"] = "value4-${appName}"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        builder.AddPlaceholderResolver(_loggerFactory);
        IConfiguration configuration = builder.Build();

        IConfigurationSection oneSection = configuration.GetSection("one");
        oneSection.Path.Should().Be("one");
        oneSection.Key.Should().Be("one");
        oneSection.Value.Should().Be("value1-test");
        oneSection.GetChildren().Should().ContainSingle();

        IConfigurationSection twoSection = oneSection.GetSection("two");
        twoSection.Path.Should().Be("one:two");
        twoSection.Key.Should().Be("two");
        twoSection.Value.Should().Be("value2-test");
        twoSection.GetChildren().Should().ContainSingle();

        IConfigurationSection threeSection = twoSection.GetSection("three");
        threeSection.Path.Should().Be("one:two:three");
        threeSection.Value.Should().Be("value3-test");
        threeSection.GetChildren().Should().ContainSingle();

        IConfigurationSection fourSection = threeSection.GetSection("four");
        fourSection.Path.Should().Be("one:two:three:four");
        fourSection.Value.Should().Be("value4-test");
        fourSection.GetChildren().Should().BeEmpty();
    }

    [Fact]
    public void Substitutes_recursive_placeholders()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["TestRoot:Key1"] = "1:FinalValue",
            ["TestRoot:Key2"] = "2:${TestRoot:Key1}",
            ["TestRoot:Key3"] = "3:${TestRoot:Key2}",
            ["TestRoot:Key4"] = "4:${TestRoot:Key3}",
            ["TestRoot:Key5"] = "5:${TestRoot:Key4}"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        builder.AddPlaceholderResolver(_loggerFactory);
        IConfiguration configuration = builder.Build();

        configuration["TestRoot:Key5"].Should().Be("5:4:3:2:1:FinalValue");
    }

    [Fact]
    public void Throws_on_self_reference()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["TestRoot:Key1"] = "${TestRoot:Key1}"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        builder.AddPlaceholderResolver(_loggerFactory);
        IConfiguration configuration = builder.Build();

        Action action = () => _ = configuration["TestRoot:Key1"];

        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("Found circular placeholder reference 'TestRoot:Key1' in configuration.");
    }

    [Fact]
    public void Throws_on_circular_reference()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["TestRoot:Key1"] = "1:${TestRoot:Key5}",
            ["TestRoot:Key2"] = "2:${TestRoot:Key1}",
            ["TestRoot:Key3"] = "3:${TestRoot:Key2}",
            ["TestRoot:Key4"] = "4:${TestRoot:Key3}",
            ["TestRoot:Key5"] = "5:${TestRoot:Key4}"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        builder.AddPlaceholderResolver(_loggerFactory);
        IConfiguration configuration = builder.Build();

        Action action = () => _ = configuration["TestRoot:Key4"];

        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("Found circular placeholder reference 'TestRoot:Key3' in configuration.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void Reloads_options_on_change(int placeholderCount)
    {
        MemoryFileProvider fileProvider = new();

        fileProvider.IncludeFile(MemoryFileProvider.DefaultAppSettingsFileName, """
        {
          "TestRoot": {
            "Value": "valueA"
          }
        }
        """);

        var builder = new ConfigurationBuilder();
        builder.AddJsonFile(fileProvider, MemoryFileProvider.DefaultAppSettingsFileName, false, true);

#pragma warning disable SA1312 // Variable names should begin with lower-case letter
        foreach (int _ in Enumerable.Repeat(0, placeholderCount))
#pragma warning restore SA1312 // Variable names should begin with lower-case letter
        {
            builder.AddPlaceholderResolver(_loggerFactory);
        }

        AssertTypesInSourceTree(builder.Sources, placeholderCount);

        IConfiguration configuration = builder.Build();

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(_loggerFactory);
        services.AddLogging();
        services.AddSingleton(configuration);
        services.Configure<TestOptions>(configuration.GetSection("TestRoot"));
        services.AddSingleton<IConfigureOptions<TestOptions>, ConfigureTestOptions>();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var configurer = (ConfigureTestOptions)serviceProvider.GetRequiredService<IConfigureOptions<TestOptions>>();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<TestOptions>>();

        configurer.ConfigureCount.Should().Be(0);
        optionsMonitor.CurrentValue.Value.Should().Be("valueA");
        _ = optionsMonitor.CurrentValue;
        configurer.ConfigureCount.Should().Be(1);

        fileProvider.ReplaceFile(MemoryFileProvider.DefaultAppSettingsFileName, """
        {
          "TestRoot": {
            "Value": "valueB"
          }
        }
        """);

        fileProvider.NotifyChanged();

        configurer.ConfigureCount.Should().Be(2);
        optionsMonitor.CurrentValue.Value.Should().Be("valueB");
        _ = optionsMonitor.CurrentValue;
        configurer.ConfigureCount.Should().Be(2);

        static void AssertTypesInSourceTree(IList<IConfigurationSource> sources, int index)
        {
            while (true)
            {
                sources.Should().ContainSingle();

                if (index > 0)
                {
                    PlaceholderConfigurationSource placeholderSource = sources[0].Should().BeOfType<PlaceholderConfigurationSource>().Subject;
                    sources = placeholderSource.Sources;
                    index -= 1;
                }
                else
                {
                    sources[0].Should().BeOfType<JsonConfigurationSource>();
                    break;
                }
            }
        }
    }

    public void Dispose()
    {
        _loggerFactory.Dispose();
    }
}
