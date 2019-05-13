﻿// Copyright 2017 the original author or authors.
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
            this.Id = id;
            this.Throws = false;
        }

        public TestContrib(string id, bool throws)
        {
            this.Id = id;
            this.Throws = throws;
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
