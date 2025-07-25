// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Prometheus;

internal sealed class ConfigurePrometheusEndpointOptions(IConfiguration configuration)
    : ConfigureEndpointOptions<PrometheusEndpointOptions>(configuration, "Management:Endpoints:Prometheus", "prometheus");
