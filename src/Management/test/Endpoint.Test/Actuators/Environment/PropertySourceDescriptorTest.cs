// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.Environment;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Environment;

public sealed class PropertySourceDescriptorTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var properties = new Dictionary<string, PropertyValueDescriptor>
        {
            { "key1", new PropertyValueDescriptor("value") },
            { "key2", new PropertyValueDescriptor(false) }
        };

        var descriptor = new PropertySourceDescriptor("name", properties);
        Assert.Equal("name", descriptor.Name);
        Assert.Same(properties, descriptor.Properties);
    }

    [Fact]
    public void JsonSerialization_ReturnsExpected()
    {
        var properties = new Dictionary<string, PropertyValueDescriptor>
        {
            { "key1", new PropertyValueDescriptor("value") },
            { "key2", new PropertyValueDescriptor(false) }
        };

        var descriptor = new PropertySourceDescriptor("name", properties);
        string result = Serialize(descriptor);

        result.Should().BeJson("""
            {
              "name": "name",
              "properties": {
                "key1": {
                  "value": "value"
                },
                "key2": {
                  "value": false
                }
              }
            }
            """);
    }
}
