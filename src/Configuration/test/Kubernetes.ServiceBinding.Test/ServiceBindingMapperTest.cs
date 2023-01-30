// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test;

public sealed class ServiceBindingMapperTest
{
    [Fact]
    public void MapFromTo_Present()
    {
        var source = new Dictionary<string, string>
        {
            { "test-source-key", "test-source-value" }
        };

        var mapper = new ServiceBindingMapper(source, string.Empty, Array.Empty<string>());
        mapper.MapFromTo("test-source-key", "test-destination-key");
        Assert.Equal("test-source-value", source["test-destination-key"]);
    }

    [Fact]
    public void MapFromTo_NotPresent()
    {
        var source = new Dictionary<string, string>();
        var mapper = new ServiceBindingMapper(source, string.Empty, Array.Empty<string>());
        mapper.MapFromTo("test-source-key", "test-destination-key");
        Assert.DoesNotContain("test-destination-key", source.Keys);
    }

    [Fact]
    public void MapFromTo_AllPresent()
    {
        var source = new Dictionary<string, string>
        {
            { "test-source-key-1", "test-source-value-1" },
            { "test-source-key-2", "test-source-value-2" },
            { "test-source-key-3", "test-source-value-3" }
        };

        var mapper = new ServiceBindingMapper(source, string.Empty, Array.Empty<string>());
        mapper.MapFromTo("test-source-key-1", "test-destination-key");
        mapper.MapFromTo("test-source-key-2", "test-destination-key");
        mapper.MapFromTo("test-source-key-3", "test-destination-key");

        Assert.Equal("test-source-value-3", source["test-destination-key"]);
    }

    [Fact]
    public void MapFromTo_NotAllPresent()
    {
        var source = new Dictionary<string, string>
        {
            { "test-source-key-1", "test-source-value-1" },
            { "test-source-key-3", "test-source-value-3" }
        };

        var mapper = new ServiceBindingMapper(source, string.Empty, Array.Empty<string>());
        mapper.MapFromTo("test-source-key-1", "test-destination-key");
        mapper.MapFromTo("test-source-key-2", "test-destination-key");
        mapper.MapFromTo("test-source-key-3", "test-destination-key");

        Assert.Equal("test-source-value-3", source["test-destination-key"]);
    }
}
