﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;
using System.Collections.Generic;

namespace Steeltoe.Common.Availability
{
    public class ReadinessHealthContributor : AvailabilityHealthContributor
    {
        public override string Id => "readiness";

        private readonly ApplicationAvailability _availability;

        public ReadinessHealthContributor(ApplicationAvailability availability)
            : base(new Dictionary<IAvailabilityState, HealthStatus> { { ReadinessState.AcceptingTraffic, HealthStatus.UP }, { ReadinessState.RefusingTraffic, HealthStatus.OUT_OF_SERVICE } })
        {
            _availability = availability;
        }

        protected override IAvailabilityState GetState() => _availability.GetReadinessState();
    }
}
