﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Health
{
    public interface IHealthRegistrationsAggregator : IHealthAggregator
    {
        Common.HealthChecks.HealthCheckResult Aggregate(IList<IHealthContributor> contributors, IOptionsMonitor<HealthCheckServiceOptions> healthServiceOptions, IServiceProvider serviceProvider);
    }
}
