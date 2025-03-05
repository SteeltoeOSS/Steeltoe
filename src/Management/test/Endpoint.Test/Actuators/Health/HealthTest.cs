// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.Health;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health;

public sealed class HealthTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesDefaults()
    {
        var health = new HealthCheckResult();
        Assert.Equal(HealthStatus.Unknown, health.Status);
        Assert.NotNull(health.Details);
        Assert.Empty(health.Details);
        Assert.Null(health.Description);
    }

    [Fact]
    public void Serialize_Default_ReturnsExpected()
    {
        var health = new HealthEndpointResponse();
        string json = Serialize(health);

        json.Should().BeJson("""
            {
              "status": "UNKNOWN"
            }
            """);
    }

    [Fact]
    public void Serialize_WithDetails_ReturnsExpected()
    {
        var health = new HealthEndpointResponse
        {
            Status = HealthStatus.OutOfService,
            Description = "Test",
            Components =
            {
                ["exampleContributor"] = new HealthCheckResult
                {
                    Details =
                    {
                        ["item1"] = new HealthData(),
                        ["item2"] = "String",
                        ["item3"] = false
                    }
                }
            }
        };

        string json = Serialize(health);

        json.Should().BeJson("""
            {
              "status": "OUT_OF_SERVICE",
              "description": "Test",
              "components": {
                "exampleContributor": {
                  "status": "UNKNOWN",
                  "details": {
                    "item1": {
                      "stringProperty": "TestData",
                      "intProperty": 100,
                      "boolProperty": true
                    },
                    "item2": "String",
                    "item3": false
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public void Serialize_WithHealthCheckResult_ReturnsExpected()
    {
        var health = new HealthEndpointResponse
        {
            Status = HealthStatus.OutOfService,
            Description = "Test",
            Components =
            {
                ["ExampleContributor"] = new HealthCheckResult
                {
                    Status = HealthStatus.Unknown,
                    Description = "ExampleDescription",
                    Details =
                    {
                        ["ExampleMessage"] = "ExampleValue"
                    }
                }
            }
        };

        string json = Serialize(health);

        json.Should().BeJson("""
            {
              "status": "OUT_OF_SERVICE",
              "description": "Test",
              "components": {
                "ExampleContributor": {
                  "status": "UNKNOWN",
                  "description": "ExampleDescription",
                  "details": {
                    "ExampleMessage": "ExampleValue"
                  }
                }
              }
            }
            """);
    }
}
