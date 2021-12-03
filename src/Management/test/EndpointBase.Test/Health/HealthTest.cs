// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Steeltoe.Management.Endpoint.Health.Test
{
    public class HealthTest : BaseTest
    {
        [Fact]
        public void Constructor_InitializesDefaults()
        {
            var health = new HealthCheckResult();
            Assert.Equal(HealthStatus.UNKNOWN, health.Status);
            Assert.NotNull(health.Details);
            Assert.Empty(health.Details);
            Assert.Null(health.Description);
        }

        [Fact]
        public void Serialize_Default_ReturnsExpected()
        {
            var health = new HealthEndpointResponse(null);
            var json = Serialize(health);
            Assert.Equal("{\"status\":\"UNKNOWN\"}", json);
        }

        [Fact]
        public void Serialize_WithDetails_ReturnsExpected()
        {
            var health = new HealthEndpointResponse(null)
            {
                Status = HealthStatus.OUT_OF_SERVICE,
                Description = "Test",
                Details = new Dictionary<string, object>()
                {
                    { "item1", new HealthData() },
                    { "item2", "String" },
                    { "item3", false }
                }
            };
            var json = Serialize(health);

            Assert.Equal("{\"status\":\"OUT_OF_SERVICE\",\"description\":\"Test\",\"details\":{\"item1\":{\"stringProperty\":\"Testdata\",\"intProperty\":100,\"boolProperty\":true},\"item2\":\"String\",\"item3\":false}}", json);
        }

        private string Serialize(HealthEndpointResponse result)
        {
            try
            {
                var options = new JsonSerializerOptions()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
#if NET5_0_OR_GREATER
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
#else
                    IgnoreNullValues = true
#endif
                };
                options.Converters.Add(new HealthConverter());

                return JsonSerializer.Serialize(result, options);
            }
            catch (Exception)
            {
            }

            return string.Empty;
        }
    }
}
