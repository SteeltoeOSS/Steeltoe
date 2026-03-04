// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.Test.Transport;

public sealed class JsonApplicationsTest
{
    [Fact]
    public void Deserialize_ApplicationSingleElement()
    {
        const string json = """
            {
              "versions__delta": "1",
              "apps__hashcode": "UP_1_",
              "application": {
                "name": "FOO"
              }
            }
            """;

        var result = JsonSerializer.Deserialize<JsonApplications>(json, EurekaClient.RequestSerializerOptions);

        result.Should().NotBeNull();
        result.VersionDelta.Should().Be(1);
        result.AppsHashCode.Should().Be("UP_1_");

        JsonApplication? app = result.Applications.Should().ContainSingle().Subject;
        app.Should().NotBeNull();
        app.Name.Should().Be("FOO");
    }

    [Fact]
    public void Deserialize_ApplicationArray()
    {
        const string json = """
            {
              "versions__delta": "1",
              "apps__hashcode": "UP_1_",
              "application": [
                {
                  "name": "FOO"
                }
              ]
            }
            """;

        var result = JsonSerializer.Deserialize<JsonApplications>(json, EurekaClient.ResponseSerializerOptions);

        result.Should().NotBeNull();
        result.VersionDelta.Should().Be(1);
        result.AppsHashCode.Should().Be("UP_1_");

        JsonApplication? app = result.Applications.Should().ContainSingle().Subject;
        app.Should().NotBeNull();
        app.Name.Should().Be("FOO");
    }
}
