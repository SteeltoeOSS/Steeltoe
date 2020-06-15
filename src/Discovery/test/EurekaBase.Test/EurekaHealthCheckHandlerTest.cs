// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
