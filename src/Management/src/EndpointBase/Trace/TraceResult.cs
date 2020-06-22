﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Trace
{
    public class TraceResult
    {
        public TraceResult(long timestamp, Dictionary<string, object> info)
        {
            TimeStamp = timestamp;
            Info = info ?? throw new ArgumentNullException(nameof(info));
        }

        [JsonProperty("timestamp")]
        public long TimeStamp { get; }

        [JsonProperty("info")]
        public Dictionary<string, object> Info { get; }
    }
}
