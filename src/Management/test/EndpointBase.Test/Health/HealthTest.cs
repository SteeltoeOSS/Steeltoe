// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
            var health = new HealthCheckResult();
            var json = Serialize(health);
            Assert.Equal("{\"status\":\"UNKNOWN\"}", json);
        }

        [Fact]
        public void Serialize_WithDetails_ReturnsExpected()
        {
            var health = new HealthCheckResult()
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

        private string Serialize(HealthCheckResult result)
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
