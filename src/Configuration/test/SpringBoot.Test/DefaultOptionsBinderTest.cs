// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Steeltoe.Configuration.SpringBoot.Test;

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

public sealed class DefaultOptionsBinderTest
{
    private static readonly bool IsDotNet10 = typeof(JsonStreamConfigurationProvider).Assembly.GetName().Version?.Major == 10;

    private static readonly Element EmptyChild = new();

    private static readonly Element NonEmptyChild = new()
    {
        NullableInt32 = 999
    };

    [Fact]
    public void Binds_composite()
    {
        const string json = """
            {
              "root": {
                "empty-array": [],
                "array-containing-null": [
                  null
                ],
                "array-containing-empty-object": [
                  {}
                ],
                "array-containing-object": [
                  {
                    "key": "value"
                  }
                ],
                "array-containing-all": [
                  null,
                  {},
                  {
                    "key": "value"
                  }
                ],
                "empty-object": {},
                "object": {
                  "key": "value"
                }
              }
            }
            """;

        MemoryStream stream = StringToStream(json);
        IConfigurationRoot configuration = new ConfigurationBuilder().AddJsonStream(stream).Build();
        JsonStreamConfigurationProvider configurationProvider = configuration.Providers.OfType<JsonStreamConfigurationProvider>().Single();

        using (new AssertionScope())
        {
            AssertGetValue(configurationProvider, "root:empty-array", IsDotNet10 ? string.Empty : null);
            AssertGetValue(configurationProvider, "root:array-containing-null:0", IsDotNet10 ? null : string.Empty);
            //AssertGetValue(configurationProvider, "root:array-containing-empty-object:0", IsDotNet10 ? string.Empty : null); // FAILS on net10.0: is null
            AssertGetValue(configurationProvider, "root:array-containing-object:0:key", "value");
            AssertGetValue(configurationProvider, "root:array-containing-all:0", IsDotNet10 ? null : string.Empty);
            //AssertGetValue(configurationProvider, "root:array-containing-all:1", IsDotNet10 ? string.Empty : null); // FAILS on net10.0: is null
            AssertGetValue(configurationProvider, "root:array-containing-all:2:key", "value");
            //AssertGetValue(configurationProvider, "root:empty-object", IsDotNet10 ? string.Empty : null); // FAILS on net10.0: is null
            AssertGetValue(configurationProvider, "root:object:key", "value");
        }
    }

    [Fact]
    public void Binds_nothing()
    {
        const string json = """
            {
              "root": {
              }
            }
            """;

        using MemoryStream stream = StringToStream(json);
        IConfigurationRoot configuration = new ConfigurationBuilder().AddJsonStream(stream).Build();
        JsonStreamConfigurationProvider configurationProvider = configuration.Providers.OfType<JsonStreamConfigurationProvider>().Single();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<Container>().BindConfiguration("root");
        using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<Container>>();
        Container container = optionsMonitor.CurrentValue;

        using (new AssertionScope())
        {
            container.NullableInt32.Should().BeNull();
            container.NullableInt32WithDefaultZero.Should().Be(0);
            container.NullableInt32WithDefaultValue.Should().Be(1);
            container.NonNullableInt32.Should().Be(0);
            container.NonNullableInt32WithDefaultZero.Should().Be(0);
            container.NonNullableInt32WithDefaultValue.Should().Be(1);

            container.NullableString.Should().BeNull();
            container.NullableStringWithDefaultEmpty.Should().BeEmpty();
            container.NullableStringWithDefaultValue.Should().Be("Value");
            container.NonNullableString.Should().BeNull();
            container.NonNullableStringWithDefaultEmpty.Should().BeEmpty();
            container.NonNullableStringWithDefaultValue.Should().Be("Value");

            container.NullableChild.Should().BeNull();
            container.NullableChildWithDefaultEmpty.Should().BeEquivalentTo(new Element());
            container.NonNullableChild.Should().BeNull();
            container.NonNullableChildWithDefaultEmpty.Should().BeEquivalentTo(new Element());

            container.NullableChildren.Should().BeNull();
            container.NullableChildrenWithDefaultEmpty.Should().BeEmpty();
            container.NonNullableChildren.Should().BeNull();
            container.NonNullableChildrenWithDefaultEmpty.Should().BeEmpty();

            container.NullableNamedChildren.Should().BeNull();
            container.NullableNamedChildrenWithDefaultEmpty.Should().BeEmpty();
            container.NonNullableNamedChildren.Should().BeNull();
            container.NonNullableNamedChildrenWithDefaultEmpty.Should().BeEmpty();

            AssertGetValue(configurationProvider, "root", null);
        }
    }

    [Fact]
    public void Binds_nulls()
    {
        const string json = """
            {
              "root": {
                "NullableInt32": null,
                "NullableInt32WithDefaultZero": null,
                "NullableInt32WithDefaultValue": null,
                "NonNullableInt32": 0,
                "NonNullableInt32WithDefaultZero": 0,
                "NonNullableInt32WithDefaultValue": 0,

                "NullableString": null,
                "NullableStringWithDefaultEmpty": null,
                "NullableStringWithDefaultValue": null,
                "NonNullableString": null,
                "NonNullableStringWithDefaultEmpty": null,
                "NonNullableStringWithDefaultValue": null,

                "NullableChild": null,
                "NullableChildWithDefaultEmpty": null,
                "NonNullableChild": null,
                "NonNullableChildWithDefaultEmpty": null,

                "NullableChildren": null,
                "NullableChildrenWithDefaultEmpty": null,
                "NonNullableChildren": null,
                "NonNullableChildrenWithDefaultEmpty": null,
                
                "NullableNamedChildren": null,
                "NullableNamedChildrenWithDefaultEmpty": null,
                "NonNullableNamedChildren": null,
                "NonNullableNamedChildrenWithDefaultEmpty": null,
              }
            }
            """;

        using MemoryStream stream = StringToStream(json);
        IConfigurationRoot configuration = new ConfigurationBuilder().AddJsonStream(stream).Build();
        JsonStreamConfigurationProvider configurationProvider = configuration.Providers.OfType<JsonStreamConfigurationProvider>().Single();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<Container>().BindConfiguration("root");
        using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<Container>>();
        Container container = optionsMonitor.CurrentValue;

        using (new AssertionScope())
        {
            container.NullableInt32.Should().BeNull();
            container.NullableInt32WithDefaultZero.Should().Be(IsDotNet10 ? null : 0);
            container.NullableInt32WithDefaultValue.Should().Be(IsDotNet10 ? null : 1);
            container.NonNullableInt32.Should().Be(0);
            container.NonNullableInt32WithDefaultZero.Should().Be(0);
            container.NonNullableInt32WithDefaultValue.Should().Be(0);

            container.NullableString.Should().Be(IsDotNet10 ? null : string.Empty);
            container.NullableStringWithDefaultEmpty.Should().Be(IsDotNet10 ? null : string.Empty);
            container.NullableStringWithDefaultValue.Should().Be(IsDotNet10 ? null : string.Empty);
            container.NonNullableString.Should().Be(IsDotNet10 ? null : string.Empty);
            container.NonNullableStringWithDefaultEmpty.Should().Be(IsDotNet10 ? null : string.Empty);
            container.NonNullableStringWithDefaultValue.Should().Be(IsDotNet10 ? null : string.Empty);

            container.NullableChild.Should().BeNull();
            //container.NullableChildWithDefaultEmpty.Should().BeNull(); // FAILS: is EmptyChild
            container.NonNullableChild.Should().BeNull();
            //container.NonNullableChildWithDefaultEmpty.Should().BeNull(); // FAILS: is EmptyChild

            container.NullableChildren.Should().BeNull();
            //container.NullableChildrenWithDefaultEmpty.Should().BeNull(); // FAILS: is []
            container.NonNullableChildren.Should().BeNull();
            //container.NonNullableChildrenWithDefaultEmpty.Should().BeNull(); // FAILS: is []

            container.NullableNamedChildren.Should().BeNull();
            //container.NullableNamedChildrenWithDefaultEmpty.Should().BeNull(); // FAILS: is []
            container.NonNullableNamedChildren.Should().BeNull();
            //container.NonNullableNamedChildrenWithDefaultEmpty.Should().BeNull(); // FAILS: is []

            AssertGetValue(configurationProvider, "root:NullableInt32", IsDotNet10 ? null : string.Empty);
            AssertGetValue(configurationProvider, "root:NullableInt32WithDefaultZero", IsDotNet10 ? null : string.Empty);
            AssertGetValue(configurationProvider, "root:NullableInt32WithDefaultValue", IsDotNet10 ? null : string.Empty);
            AssertGetValue(configurationProvider, "root:NonNullableInt32", "0");
            AssertGetValue(configurationProvider, "root:NonNullableInt32WithDefaultZero", "0");
            AssertGetValue(configurationProvider, "root:NonNullableInt32WithDefaultValue", "0");

            AssertGetValue(configurationProvider, "root:NullableString", IsDotNet10 ? null : string.Empty);
            AssertGetValue(configurationProvider, "root:NullableStringWithDefaultEmpty", IsDotNet10 ? null : string.Empty);
            AssertGetValue(configurationProvider, "root:NullableStringWithDefaultValue", IsDotNet10 ? null : string.Empty);
            AssertGetValue(configurationProvider, "root:NonNullableString", IsDotNet10 ? null : string.Empty);
            AssertGetValue(configurationProvider, "root:NonNullableStringWithDefaultEmpty", IsDotNet10 ? null : string.Empty);
            AssertGetValue(configurationProvider, "root:NonNullableStringWithDefaultValue", IsDotNet10 ? null : string.Empty);

            AssertGetValue(configurationProvider, "root:NullableChild", IsDotNet10 ? null : string.Empty);
            AssertGetValue(configurationProvider, "root:NullableChildWithDefaultEmpty", IsDotNet10 ? null : string.Empty);
            AssertGetValue(configurationProvider, "root:NonNullableChild", IsDotNet10 ? null : string.Empty);
            AssertGetValue(configurationProvider, "root:NonNullableChildWithDefaultEmpty", IsDotNet10 ? null : string.Empty);

            AssertGetValue(configurationProvider, "root:NullableChildren", IsDotNet10 ? null : string.Empty);
            AssertGetValue(configurationProvider, "root:NullableChildrenWithDefaultEmpty", IsDotNet10 ? null : string.Empty);
            AssertGetValue(configurationProvider, "root:NonNullableChildren", IsDotNet10 ? null : string.Empty);
            AssertGetValue(configurationProvider, "root:NonNullableChildrenWithDefaultEmpty", IsDotNet10 ? null : string.Empty);

            AssertGetValue(configurationProvider, "root:NullableNamedChildren", IsDotNet10 ? null : string.Empty);
            AssertGetValue(configurationProvider, "root:NullableNamedChildrenWithDefaultEmpty", IsDotNet10 ? null : string.Empty);
            AssertGetValue(configurationProvider, "root:NonNullableNamedChildren", IsDotNet10 ? null : string.Empty);
            AssertGetValue(configurationProvider, "root:NonNullableNamedChildrenWithDefaultEmpty", IsDotNet10 ? null : string.Empty);
        }
    }

    [Fact]
    public void Binds_empty_object_or_collection()
    {
        const string json = """
            {
              "root": {
                "NullableChild": {},
                "NullableChildWithDefaultEmpty": {},
                "NonNullableChild": {},
                "NonNullableChildWithDefaultEmpty": {},

                "NullableChildren": [],
                "NullableChildrenWithDefaultEmpty": [],
                "NonNullableChildren": [],
                "NonNullableChildrenWithDefaultEmpty": [],
                
                "NullableNamedChildren": {},
                "NullableNamedChildrenWithDefaultEmpty": {},
                "NonNullableNamedChildren": {},
                "NonNullableNamedChildrenWithDefaultEmpty": {},
              }
            }
            """;

        using MemoryStream stream = StringToStream(json);
        IConfigurationRoot configuration = new ConfigurationBuilder().AddJsonStream(stream).Build();
        JsonStreamConfigurationProvider configurationProvider = configuration.Providers.OfType<JsonStreamConfigurationProvider>().Single();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<Container>().BindConfiguration("root");
        using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<Container>>();
        Container container = optionsMonitor.CurrentValue;

        using (new AssertionScope())
        {
            //container.NullableChild.Should().BeEquivalentTo(EmptyChild); // FAILS: is null
            container.NullableChildWithDefaultEmpty.Should().BeEquivalentTo(EmptyChild);
            //container.NonNullableChild.Should().BeEquivalentTo(EmptyChild); // FAILS: is null
            container.NonNullableChildWithDefaultEmpty.Should().BeEquivalentTo(EmptyChild);

            //container.NullableChildren.Should().BeEmpty(); // FAILS: is null
            container.NullableChildrenWithDefaultEmpty.Should().BeEmpty();
            //container.NonNullableChildren.Should().BeEmpty(); // FAILS: is null
            container.NonNullableChildrenWithDefaultEmpty.Should().BeEmpty();

            //container.NullableNamedChildren.Should().BeEmpty(); // FAILS: is null
            container.NullableNamedChildrenWithDefaultEmpty.Should().BeEmpty();
            //container.NonNullableNamedChildren.Should().BeEmpty(); // FAILS: is null
            container.NonNullableNamedChildrenWithDefaultEmpty.Should().BeEmpty();

#if NET10_0_OR_GREATER
#else
            AssertGetValue(configurationProvider, "root:NullableChild", IsDotNet10 ? string.Empty : null); // FAILS on net10.0: is null
            AssertGetValue(configurationProvider, "root:NullableChildWithDefaultEmpty", IsDotNet10 ? string.Empty : null); // FAILS on net10.0: is null
            AssertGetValue(configurationProvider, "root:NonNullableChild", IsDotNet10 ? string.Empty : null); // FAILS on net10.0: is null
            AssertGetValue(configurationProvider, "root:NonNullableChildWithDefaultEmpty", IsDotNet10 ? string.Empty : null); // FAILS on net10.0: is null
#endif

            AssertGetValue(configurationProvider, "root:NullableChildren", IsDotNet10 ? string.Empty : null);
            AssertGetValue(configurationProvider, "root:NullableChildrenWithDefaultEmpty", IsDotNet10 ? string.Empty : null);
            AssertGetValue(configurationProvider, "root:NonNullableChildren", IsDotNet10 ? string.Empty : null);
            AssertGetValue(configurationProvider, "root:NonNullableChildrenWithDefaultEmpty", IsDotNet10 ? string.Empty : null);

#if NET10_0_OR_GREATER
#else
            AssertGetValue(configurationProvider, "root:NullableNamedChildren", IsDotNet10 ? string.Empty : null); // FAILS on net10.0: is null
            AssertGetValue(configurationProvider, "root:NullableNamedChildrenWithDefaultEmpty", IsDotNet10 ? string.Empty : null); // FAILS on net10.0: is null
            AssertGetValue(configurationProvider, "root:NonNullableNamedChildren", IsDotNet10 ? string.Empty : null); // FAILS on net10.0: is null
            AssertGetValue(configurationProvider, "root:NonNullableNamedChildrenWithDefaultEmpty", IsDotNet10 ? string.Empty : null); // FAILS on net10.0: is null
#endif
        }
    }

    [Fact]
    public void Binds_collection_containing_empty_object()
    {
        const string json = """
            {
              "root": {
                "NullableChildren": [
                  {}
                ],
                "NullableChildrenWithDefaultEmpty": [
                  {}
                ],
                "NonNullableChildren": [
                  {}
                ],
                "NonNullableChildrenWithDefaultEmpty": [
                  {}
                ],
                
                "NullableNamedChildren": {
                  "TestKey": {}
                },
                "NullableNamedChildrenWithDefaultEmpty": {
                  "TestKey": {}
                },
                "NonNullableNamedChildren": {
                  "TestKey": {}
                },
                "NonNullableNamedChildrenWithDefaultEmpty": {
                  "TestKey": {}
                }
              }
            }
            """;

        using MemoryStream stream = StringToStream(json);
        IConfigurationRoot configuration = new ConfigurationBuilder().AddJsonStream(stream).Build();
#if NET10_0_OR_GREATER
#else
        JsonStreamConfigurationProvider configurationProvider = configuration.Providers.OfType<JsonStreamConfigurationProvider>().Single();
#endif

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<Container>().BindConfiguration("root");
        using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<Container>>();
        Container container = optionsMonitor.CurrentValue;

        using (new AssertionScope())
        {
            container.NullableChildren.Should().ContainSingle().Subject.Should().BeEquivalentTo(EmptyChild);
            container.NullableChildrenWithDefaultEmpty.Should().ContainSingle().Subject.Should().BeEquivalentTo(EmptyChild);
            container.NonNullableChildren.Should().ContainSingle().Subject.Should().BeEquivalentTo(EmptyChild);
            container.NonNullableChildrenWithDefaultEmpty.Should().ContainSingle().Subject.Should().BeEquivalentTo(EmptyChild);

            container.NullableNamedChildren.Should().ContainKey("TestKey").WhoseValue.Should().BeEquivalentTo(EmptyChild);
            container.NullableNamedChildrenWithDefaultEmpty.Should().ContainKey("TestKey").WhoseValue.Should().BeEquivalentTo(EmptyChild);
            container.NonNullableNamedChildren.Should().ContainKey("TestKey").WhoseValue.Should().BeEquivalentTo(EmptyChild);
            container.NonNullableNamedChildrenWithDefaultEmpty.Should().ContainKey("TestKey").WhoseValue.Should().BeEquivalentTo(EmptyChild);

#if NET10_0_OR_GREATER
#else
            AssertGetValue(configurationProvider, "root:NullableChildren:0", IsDotNet10 ? string.Empty : null); // FAILS on net10.0: is null
            AssertGetValue(configurationProvider, "root:NullableChildrenWithDefaultEmpty:0", IsDotNet10 ? string.Empty : null); // FAILS on net10.0: is null
            AssertGetValue(configurationProvider, "root:NonNullableChildren:0", IsDotNet10 ? string.Empty : null); // FAILS on net10.0: is null
            AssertGetValue(configurationProvider, "root:NonNullableChildrenWithDefaultEmpty:0", IsDotNet10 ? string.Empty : null); // FAILS on net10.0: is null

            AssertGetValue(configurationProvider, "root:NullableNamedChildren:TestKey", IsDotNet10 ? string.Empty : null); // FAILS on net10.0: is null
            AssertGetValue(configurationProvider, "root:NullableNamedChildrenWithDefaultEmpty:TestKey", IsDotNet10 ? string.Empty : null); // FAILS on net10.0: is null
            AssertGetValue(configurationProvider, "root:NonNullableNamedChildren:TestKey", IsDotNet10 ? string.Empty : null); // FAILS on net10.0: is null
            AssertGetValue(configurationProvider, "root:NonNullableNamedChildrenWithDefaultEmpty:TestKey", IsDotNet10 ? string.Empty : null); // FAILS on net10.0: is null
#endif
        }
    }

    [Fact]
    public void Binds_custom_values_to_options()
    {
        const string json = """
            {
              "root": {
                "NullableInt32": 111,
                "NullableInt32WithDefaultZero": 222,
                "NullableInt32WithDefaultValue": 333,
                "NonNullableInt32": 444,
                "NonNullableInt32WithDefaultZero": 555,
                "NonNullableInt32WithDefaultValue": 666,

                "NullableString": "Test1",
                "NullableStringWithDefaultEmpty": "Test2",
                "NullableStringWithDefaultValue": "Test3",
                "NonNullableString": "Test4",
                "NonNullableStringWithDefaultEmpty": "Test5",
                "NonNullableStringWithDefaultValue": "Test6",

                "NullableChild": {
                  "NullableInt32": 999
                },
                "NullableChildWithDefaultEmpty": {
                  "NullableInt32": 999
                },
                "NonNullableChild": {
                  "NullableInt32": 999
                },
                "NonNullableChildWithDefaultEmpty": {
                  "NullableInt32": 999
                },

                "NullableChildren": [
                  {
                    "NullableInt32": 999
                  }
                ],
                "NullableChildrenWithDefaultEmpty": [
                  {
                    "NullableInt32": 999
                  }
                ],
                "NonNullableChildren": [
                  {
                    "NullableInt32": 999
                  }
                ],
                "NonNullableChildrenWithDefaultEmpty": [
                  {
                    "NullableInt32": 999
                  }
                ],
                
                "NullableNamedChildren": {
                  "TestKey": {
                    "NullableInt32": 999
                  }
                },
                "NullableNamedChildrenWithDefaultEmpty": {
                  "TestKey": {
                    "NullableInt32": 999
                  }
                },
                "NonNullableNamedChildren": {
                  "TestKey": {
                    "NullableInt32": 999
                  }
                },
                "NonNullableNamedChildrenWithDefaultEmpty": {
                  "TestKey": {
                    "NullableInt32": 999
                  }
                },
              }
            }
            """;

        using MemoryStream stream = StringToStream(json);
        IConfigurationRoot configuration = new ConfigurationBuilder().AddJsonStream(stream).Build();
        JsonStreamConfigurationProvider configurationProvider = configuration.Providers.OfType<JsonStreamConfigurationProvider>().Single();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<Container>().BindConfiguration("root");
        using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<Container>>();
        Container container = optionsMonitor.CurrentValue;

        using (new AssertionScope())
        {
            container.NullableInt32.Should().Be(111);
            container.NullableInt32WithDefaultZero.Should().Be(222);
            container.NullableInt32WithDefaultValue.Should().Be(333);
            container.NonNullableInt32.Should().Be(444);
            container.NonNullableInt32WithDefaultZero.Should().Be(555);
            container.NonNullableInt32WithDefaultValue.Should().Be(666);

            container.NullableString.Should().Be("Test1");
            container.NullableStringWithDefaultEmpty.Should().Be("Test2");
            container.NullableStringWithDefaultValue.Should().Be("Test3");
            container.NonNullableString.Should().Be("Test4");
            container.NonNullableStringWithDefaultEmpty.Should().Be("Test5");
            container.NonNullableStringWithDefaultValue.Should().Be("Test6");

            container.NullableChild.Should().BeEquivalentTo(NonEmptyChild);
            container.NullableChildWithDefaultEmpty.Should().BeEquivalentTo(NonEmptyChild);
            container.NonNullableChild.Should().BeEquivalentTo(NonEmptyChild);
            container.NonNullableChildWithDefaultEmpty.Should().BeEquivalentTo(NonEmptyChild);

            container.NullableChildren.Should().ContainSingle().Which.Should().BeEquivalentTo(NonEmptyChild);
            container.NullableChildrenWithDefaultEmpty.Should().ContainSingle().Which.Should().BeEquivalentTo(NonEmptyChild);
            container.NonNullableChildren.Should().ContainSingle().Which.Should().BeEquivalentTo(NonEmptyChild);
            container.NonNullableChildrenWithDefaultEmpty.Should().ContainSingle().Which.Should().BeEquivalentTo(NonEmptyChild);

            container.NullableNamedChildren.Should().ContainKey("TestKey").WhoseValue.Should().BeEquivalentTo(NonEmptyChild);
            container.NullableNamedChildrenWithDefaultEmpty.Should().ContainKey("TestKey").WhoseValue.Should().BeEquivalentTo(NonEmptyChild);
            container.NonNullableNamedChildren.Should().ContainKey("TestKey").WhoseValue.Should().BeEquivalentTo(NonEmptyChild);
            container.NonNullableNamedChildrenWithDefaultEmpty.Should().ContainKey("TestKey").WhoseValue.Should().BeEquivalentTo(NonEmptyChild);

            AssertGetValue(configurationProvider, "root:NullableInt32", "111");
            AssertGetValue(configurationProvider, "root:NullableInt32WithDefaultZero", "222");
            AssertGetValue(configurationProvider, "root:NullableInt32WithDefaultValue", "333");
            AssertGetValue(configurationProvider, "root:NonNullableInt32", "444");
            AssertGetValue(configurationProvider, "root:NonNullableInt32WithDefaultZero", "555");
            AssertGetValue(configurationProvider, "root:NonNullableInt32WithDefaultValue", "666");

            AssertGetValue(configurationProvider, "root:NullableString", "Test1");
            AssertGetValue(configurationProvider, "root:NullableStringWithDefaultEmpty", "Test2");
            AssertGetValue(configurationProvider, "root:NullableStringWithDefaultValue", "Test3");
            AssertGetValue(configurationProvider, "root:NonNullableString", "Test4");
            AssertGetValue(configurationProvider, "root:NonNullableStringWithDefaultEmpty", "Test5");
            AssertGetValue(configurationProvider, "root:NonNullableStringWithDefaultValue", "Test6");

            AssertGetValue(configurationProvider, "root:NullableChild:NullableInt32", "999");
            AssertGetValue(configurationProvider, "root:NullableChildWithDefaultEmpty:NullableInt32", "999");
            AssertGetValue(configurationProvider, "root:NonNullableChild:NullableInt32", "999");
            AssertGetValue(configurationProvider, "root:NonNullableChildWithDefaultEmpty:NullableInt32", "999");

            AssertGetValue(configurationProvider, "root:NullableChildren:0:NullableInt32", "999");
            AssertGetValue(configurationProvider, "root:NullableChildrenWithDefaultEmpty:0:NullableInt32", "999");
            AssertGetValue(configurationProvider, "root:NonNullableChildren:0:NullableInt32", "999");
            AssertGetValue(configurationProvider, "root:NonNullableChildrenWithDefaultEmpty:0:NullableInt32", "999");

            AssertGetValue(configurationProvider, "root:NullableNamedChildren:TestKey:NullableInt32", "999");
            AssertGetValue(configurationProvider, "root:NullableNamedChildrenWithDefaultEmpty:TestKey:NullableInt32", "999");
            AssertGetValue(configurationProvider, "root:NonNullableNamedChildren:TestKey:NullableInt32", "999");
            AssertGetValue(configurationProvider, "root:NonNullableNamedChildrenWithDefaultEmpty:TestKey:NullableInt32", "999");
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
}

public class Element
{
    public int? NullableInt32 { get; set; }
    public int? NullableInt32WithDefaultZero { get; set; } = 0;
    public int? NullableInt32WithDefaultValue { get; set; } = 1;
    public int NonNullableInt32 { get; set; }
    public int NonNullableInt32WithDefaultZero { get; set; }
    public int NonNullableInt32WithDefaultValue { get; set; } = 1;

    public string? NullableString { get; set; }
    public string? NullableStringWithDefaultEmpty { get; set; } = string.Empty;
    public string? NullableStringWithDefaultValue { get; set; } = "Value";
    public string NonNullableString { get; set; } = null!;
    public string NonNullableStringWithDefaultEmpty { get; set; } = string.Empty;
    public string NonNullableStringWithDefaultValue { get; set; } = "Value";
}

public sealed class Container : Element
{
    public Element? NullableChild { get; set; }
    public Element? NullableChildWithDefaultEmpty { get; set; } = new();
    public Element NonNullableChild { get; set; } = null!;
    public Element NonNullableChildWithDefaultEmpty { get; set; } = new();

    public IList<Element>? NullableChildren { get; set; }
    public IList<Element>? NullableChildrenWithDefaultEmpty { get; set; } = new List<Element>();
    public IList<Element> NonNullableChildren { get; set; } = null!;
    public IList<Element> NonNullableChildrenWithDefaultEmpty { get; set; } = new List<Element>();

    public IDictionary<string, Element>? NullableNamedChildren { get; set; }
    public IDictionary<string, Element>? NullableNamedChildrenWithDefaultEmpty { get; set; } = new Dictionary<string, Element>();
    public IDictionary<string, Element> NonNullableNamedChildren { get; set; } = null!;
    public IDictionary<string, Element> NonNullableNamedChildrenWithDefaultEmpty { get; set; } = new Dictionary<string, Element>();
}
