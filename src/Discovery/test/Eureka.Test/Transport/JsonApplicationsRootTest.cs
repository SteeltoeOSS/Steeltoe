// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.Test.Transport;

public sealed class JsonApplicationsRootTest
{
    [Fact]
    public void Deserialize()
    {
        const string json = """
            {
              "applications": {}
            }
            """;

        JsonApplicationsRoot? result = JsonSerializer.Deserialize(json, EurekaJsonSerializerContext.Default.JsonApplicationsRoot);

        result.Should().NotBeNull();
        result.Applications.Should().NotBeNull();
    }
}
