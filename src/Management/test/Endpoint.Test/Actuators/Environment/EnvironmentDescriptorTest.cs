// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.Environment;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Environment;

public sealed class EnvironmentDescriptorTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        List<string> profiles = [];
        List<PropertySourceDescriptor> propertySourceDescriptors = [];
        var environmentDescriptor = new EnvironmentResponse(profiles, propertySourceDescriptors);
        Assert.Same(profiles, environmentDescriptor.ActiveProfiles);
        Assert.Same(propertySourceDescriptors, environmentDescriptor.PropertySources);
    }

    [Fact]
    public void JsonSerialization_ReturnsExpected()
    {
        List<string> profiles = ["foobar"];

        var properties = new Dictionary<string, PropertyValueDescriptor>
        {
            ["key1"] = new("value"),
            ["key2"] = new(false)
        };

        List<PropertySourceDescriptor> propertySourceDescriptors = [new("name", properties)];

        var environmentDescriptor = new EnvironmentResponse(profiles, propertySourceDescriptors);
        string result = Serialize(environmentDescriptor);

        result.Should().BeJson("""
            {
              "activeProfiles": [
                "foobar"
              ],
              "propertySources": [
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
              ]
            }
            """);
    }
}
