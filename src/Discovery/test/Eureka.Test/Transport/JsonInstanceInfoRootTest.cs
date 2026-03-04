// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.Test.Transport;

public sealed class JsonInstanceInfoRootTest
{
    [Fact]
    public void Serialize()
    {
        var root = new JsonInstanceInfoRoot
        {
            Instance = new JsonInstanceInfo()
        };

        string result = JsonSerializer.Serialize(root, EurekaClient.RequestSerializerOptions);

        result.Should().BeJson("""
            {
              "instance": {}
            }
            """);
    }

    [Fact]
    public void Deserialize()
    {
        const string json = """
            {
              "instance": {}
            }
            """;

        var result = JsonSerializer.Deserialize<JsonInstanceInfoRoot>(json, EurekaClient.ResponseSerializerOptions);

        result.Should().NotBeNull();
        result.Instance.Should().NotBeNull();
    }
}
