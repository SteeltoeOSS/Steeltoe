// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.Test.Transport;

public sealed class JsonApplicationTest
{
    [Fact]
    public void Deserialize_InstanceArray()
    {
        const string json = """
            {
              "name": "FOO",
              "instance": [
                {
                  "instanceId": "localhost:foo"
                }
              ]
            }
            """;

        JsonApplication? result = JsonSerializer.Deserialize(json, EurekaJsonSerializerContext.Default.JsonApplication);

        result.Should().NotBeNull();
        result.Name.Should().Be("FOO");

        JsonInstanceInfo? instance = result.Instances.Should().ContainSingle().Subject;
        instance.Should().NotBeNull();
        instance.InstanceId.Should().Be("localhost:foo");
    }

    [Fact]
    public void Deserialize_InstanceSingleElement()
    {
        const string json = """
            {
              "name": "FOO",
              "instance": {
                "instanceId": "localhost:foo"
              }
            }
            """;

        JsonApplication? result = JsonSerializer.Deserialize(json, EurekaJsonSerializerContext.Default.JsonApplication);

        result.Should().NotBeNull();
        result.Name.Should().Be("FOO");

        JsonInstanceInfo? instance = result.Instances.Should().ContainSingle().Subject;
        instance.Should().NotBeNull();
        instance.InstanceId.Should().Be("localhost:foo");
    }
}
