// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
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
            Assert.Equal("{\"status\":\"OUT_OF_SERVICE\",\"description\":\"Test\",\"item1\":{\"stringProperty\":\"Testdata\",\"intProperty\":100,\"boolProperty\":true},\"item2\":\"String\",\"item3\":false}", json);
        }

        private string Serialize(HealthEndpointResponse result)
        {
            try
            {
                var serializerSettings = new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() }
                };
                serializerSettings.Converters.Add(new HealthJsonConverter());

                return JsonConvert.SerializeObject(result, serializerSettings);
            }
            catch (Exception)
            {
            }

            return string.Empty;
        }
    }
}
