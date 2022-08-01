// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Test;
using Xunit;

namespace Steeltoe.Management.Endpoint.Env.Test;

public class EnvironmentDescriptorTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var profiles = new List<string>();
        var propertySourceDescriptors = new List<PropertySourceDescriptor>();
        var environmentDescriptor = new EnvironmentDescriptor(profiles, propertySourceDescriptors);
        Assert.Same(profiles, environmentDescriptor.ActiveProfiles);
        Assert.Same(propertySourceDescriptors, environmentDescriptor.PropertySources);
    }

    [Fact]
    public void JsonSerialization_ReturnsExpected()
    {
        var profiles = new List<string> { "foobar" };
        var properties = new Dictionary<string, PropertyValueDescriptor>
        {
            { "key1", new PropertyValueDescriptor("value") },
            { "key2", new PropertyValueDescriptor(false) },
        };
        var propertySourceDescriptors = new List<PropertySourceDescriptor>
        {
            new ("name", properties)
        };
        var environmentDescriptor = new EnvironmentDescriptor(profiles, propertySourceDescriptors);
        var result = Serialize(environmentDescriptor);
        Assert.Equal("{\"activeProfiles\":[\"foobar\"],\"propertySources\":[{\"name\":\"name\",\"properties\":{\"key1\":{\"value\":\"value\"},\"key2\":{\"value\":false}}}]}", result);
    }
}
