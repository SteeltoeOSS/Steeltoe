// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;
using System;

namespace Steeltoe.Management.Endpoint.Health.Test
{
    internal class TestContrib : IHealthContributor
    {
#pragma warning disable SA1401 // Fields must be private
        public bool Called = false;
        public bool Throws = false;
#pragma warning restore SA1401 // Fields must be private

        public TestContrib(string id)
        {
            Id = id;
            Throws = false;
        }

        public TestContrib(string id, bool throws)
        {
            Id = id;
            Throws = throws;
        }

        public string Id { get; private set; }

        public HealthCheckResult Health()
        {
            if (Throws)
            {
                throw new Exception();
            }

            Called = true;
            return new HealthCheckResult()
            {
                Status = HealthStatus.UP
            };
        }
    }
}
