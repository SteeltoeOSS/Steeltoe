// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Test;
using Xunit;

namespace Steeltoe.Management.Endpoint.Env.Test;

public class PropertyValueDescriptorTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var property = new PropertyValueDescriptor("value", "origin");

        Assert.Equal("value", property.Value);
        Assert.Equal("origin", property.Origin);
    }

    [Fact]
    public void JsonSerialization_ReturnsExpected()
    {
        var property = new PropertyValueDescriptor("value", "origin");
        string result = Serialize(property);
        Assert.Equal("{\"value\":\"value\",\"origin\":\"origin\"}", result);
    }
}
