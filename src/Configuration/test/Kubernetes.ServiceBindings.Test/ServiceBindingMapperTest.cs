// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.Kubernetes.ServiceBindings.Test;

public sealed class ServiceBindingMapperTest
{
    [Fact]
    public void MapFromTo_Present()
    {
        var source = new Dictionary<string, string?>
        {
            ["test-source-key"] = "test-source-value"
        };

        var mapper = new ServiceBindingMapper(source, string.Empty);
        string? newValue = mapper.MapFromTo("test-source-key", "test-destination-key");

        newValue.Should().Be("test-source-value");
        source["test-destination-key"].Should().Be("test-source-value");
    }

    [Fact]
    public void MapFromTo_NotPresent_SetNull()
    {
        var source = new Dictionary<string, string?>();
        var mapper = new ServiceBindingMapper(source, string.Empty);
        string? newValue = mapper.MapFromTo("test-source-key", "test-destination-key");

        newValue.Should().BeNull();
        source["test-destination-key"].Should().BeNull();
    }

    [Fact]
    public void MapFromTo_OverwritesExisting()
    {
        var source = new Dictionary<string, string?>
        {
            ["test-source-key-1"] = "test-source-value-1",
            ["test-source-key-2"] = "test-source-value-2",
            ["test-source-key-3"] = "test-source-value-3"
        };

        var mapper = new ServiceBindingMapper(source, string.Empty);
        mapper.MapFromTo("test-source-key-1", "test-destination-key");
        mapper.MapFromTo("test-source-key-2", "test-destination-key");
        mapper.MapFromTo("test-source-key-3", "test-destination-key");

        source["test-destination-key"].Should().Be("test-source-value-3");
    }

    [Fact]
    public void MapFromAppendTo_Present()
    {
        var source = new Dictionary<string, string?>
        {
            ["test-destination-key"] = "test-source-value-1",
            ["test-source-key"] = "test-source-value-2"
        };

        var mapper = new ServiceBindingMapper(source, string.Empty);
        string? newValue = mapper.MapFromAppendTo("test-source-key", "test-destination-key", " AND ");

        newValue.Should().Be("test-source-value-1 AND test-source-value-2");
        source["test-destination-key"].Should().Be("test-source-value-1 AND test-source-value-2");
    }

    [Fact]
    public void MapFromAppendTo_FromKeyNotPresent_DoesNothing()
    {
        var source = new Dictionary<string, string?>
        {
            ["test-destination-key"] = "test-source-value"
        };

        var mapper = new ServiceBindingMapper(source, string.Empty);
        string? newValue = mapper.MapFromAppendTo("test-source-key", "test-destination-key", " AND ");

        newValue.Should().BeNull();
        source["test-destination-key"].Should().Be("test-source-value");
    }

    [Fact]
    public void MapFromAppendTo_AppendToKeyNotPresent_DoesNothing()
    {
        var source = new Dictionary<string, string?>
        {
            ["test-source-key"] = "test-source-value"
        };

        var mapper = new ServiceBindingMapper(source, string.Empty);
        string? newValue = mapper.MapFromAppendTo("test-source-key", "test-destination-key", " AND ");

        newValue.Should().BeNull();
        source.Should().NotContainKey("test-destination-key");
    }

    [Fact]
    public void MapFromToFile_Present()
    {
        var source = new Dictionary<string, string?>
        {
            ["test-source-key"] = "test-source-value"
        };

        var mapper = new ServiceBindingMapper(source, string.Empty);
        string? tempPath = mapper.MapFromToFile("test-source-key", "test-destination-key");

        tempPath.Should().NotBeNull();
        source["test-destination-key"].Should().Be(tempPath);
        File.ReadAllText(tempPath).Should().Be("test-source-value");

        File.Delete(tempPath);
    }

    [Fact]
    public void MapFromToFile_NotPresent_SetNull()
    {
        var source = new Dictionary<string, string?>();
        var mapper = new ServiceBindingMapper(source, string.Empty);
        string? tempPath = mapper.MapFromToFile("test-source-key", "test-destination-key");

        tempPath.Should().BeNull();
        source["test-destination-key"].Should().BeNull();
    }
}
