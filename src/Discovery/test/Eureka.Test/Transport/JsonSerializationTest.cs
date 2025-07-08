// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.Test.Transport;

public sealed class JsonSerializationTest
{
    [Fact]
    public void Deserialize_BadJson_Throws()
    {
#pragma warning disable JSON001 // Invalid JSON pattern
        const string json = """
            {
                'instanceId':'localhost:foo',
                'hostName':'localhost',
                'app':'FOO',
                'ipAddr':'192.168.56.1',
            """;
#pragma warning restore JSON001 // Invalid JSON pattern

        Action action = () => JsonSerializer.Deserialize<JsonInstanceInfo>(json);

        action.Should().ThrowExactly<JsonException>();
    }
}
