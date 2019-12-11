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

using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Discovery.Eureka.AppInfo;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test
{
    public class EurekaHealthCheckHandlerTest
    {
        [Fact]
        public void MapToInstanceStatus_ReturnsExpected()
        {
            EurekaHealthCheckHandler handler = new EurekaHealthCheckHandler();
            Assert.Equal(InstanceStatus.DOWN, handler.MapToInstanceStatus(HealthStatus.DOWN));
            Assert.Equal(InstanceStatus.UP, handler.MapToInstanceStatus(HealthStatus.UP));
            Assert.Equal(InstanceStatus.UNKNOWN, handler.MapToInstanceStatus(HealthStatus.WARNING));
            Assert.Equal(InstanceStatus.UNKNOWN, handler.MapToInstanceStatus(HealthStatus.UNKNOWN));
            Assert.Equal(InstanceStatus.OUT_OF_SERVICE, handler.MapToInstanceStatus(HealthStatus.OUT_OF_SERVICE));
        }

        [Fact]
        public void DoHealthChecks_ReturnsExpected()
        {
            EurekaHealthCheckHandler handler = new EurekaHealthCheckHandler();
            var result = handler.DoHealthChecks(new List<IHealthContributor>());
            Assert.Empty(result);

            result = handler.DoHealthChecks(new List<IHealthContributor>() { new TestContributor() });
            Assert.Empty(result);
        }

        [Fact]
        public void AggregateStatus_ReturnsExpected()
        {
            EurekaHealthCheckHandler handler = new EurekaHealthCheckHandler();

            List<HealthCheckResult> results = new List<HealthCheckResult>();
            Assert.Equal(HealthStatus.UNKNOWN, handler.AggregateStatus(results));

            results = new List<HealthCheckResult>()
            {
                new HealthCheckResult()
                {
                    Status = HealthStatus.DOWN
                },
                new HealthCheckResult()
                {
                    Status = HealthStatus.UP
                }
            };
            Assert.Equal(HealthStatus.DOWN, handler.AggregateStatus(results));
            results = new List<HealthCheckResult>()
            {
                new HealthCheckResult()
                {
                    Status = HealthStatus.UP
                },
                new HealthCheckResult()
                {
                    Status = HealthStatus.DOWN
                }
            };
            Assert.Equal(HealthStatus.DOWN, handler.AggregateStatus(results));

            results = new List<HealthCheckResult>()
            {
                new HealthCheckResult()
                {
                    Status = HealthStatus.UP
                },
                new HealthCheckResult()
                {
                    Status = HealthStatus.OUT_OF_SERVICE
                }
            };
            Assert.Equal(HealthStatus.OUT_OF_SERVICE, handler.AggregateStatus(results));

            results = new List<HealthCheckResult>()
            {
                new HealthCheckResult()
                {
                    Status = HealthStatus.UP
                },
                new HealthCheckResult()
                {
                    Status = HealthStatus.WARNING
                }
            };

            Assert.Equal(HealthStatus.UP, handler.AggregateStatus(results));
            results = new List<HealthCheckResult>()
            {
                new HealthCheckResult()
                {
                    Status = HealthStatus.WARNING
                },
                new HealthCheckResult()
                {
                    Status = HealthStatus.WARNING
                }
            };
            Assert.Equal(HealthStatus.UNKNOWN, handler.AggregateStatus(results));
        }

        public class TestContributor : IHealthContributor
        {
            public string Id => "TestContrib";

            public HealthCheckResult Health()
            {
                throw new NotImplementedException();
            }
        }
    }
}
