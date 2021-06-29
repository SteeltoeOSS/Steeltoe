﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Metrics
{
    public class MetricsListNamesResponse : IMetricsResponse
    {
        [JsonPropertyName("names")]
        public ISet<string> Names { get; }

        public MetricsListNamesResponse(ISet<string> names)
        {
            Names = names;
        }
    }
}
