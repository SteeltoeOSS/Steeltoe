// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Test;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Env.Test;

public class EnvironmentDescriptorTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var profiles = new List<string>();
        var propDescs = new List<PropertySourceDescriptor>();
        var desc = new EnvironmentDescriptor(profiles, propDescs);
        Assert.Same(profiles, desc.ActiveProfiles);
        Assert.Same(propDescs, desc.PropertySources);
    }

    [Fact]
    public void JsonSerialization_ReturnsExpected()
    {
        var profiles = new List<string>() { "foobar" };
        var properties = new Dictionary<string, PropertyValueDescriptor>()
        {
            { "key1", new PropertyValueDescriptor("value") },
            { "key2", new PropertyValueDescriptor(false) },
        };
        var propDescs = new List<PropertySourceDescriptor>()
        {
            new PropertySourceDescriptor("name", properties)
        };
        var desc = new EnvironmentDescriptor(profiles, propDescs);
        var result = Serialize(desc);
        Assert.Equal("{\"activeProfiles\":[\"foobar\"],\"propertySources\":[{\"name\":\"name\",\"properties\":{\"key1\":{\"value\":\"value\"},\"key2\":{\"value\":false}}}]}", result);
    }
}