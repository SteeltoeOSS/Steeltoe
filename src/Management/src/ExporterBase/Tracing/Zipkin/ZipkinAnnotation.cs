// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using System;

namespace Steeltoe.Management.Exporter.Tracing.Zipkin
{
    [Obsolete("Use OpenCensus project packages")]
    internal class ZipkinAnnotation
    {
        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (!(o is ZipkinAnnotation))
            {
                return false;
            }

            var that = (ZipkinAnnotation)o;
            return (Timestamp == that.Timestamp) && Value.Equals(that.Value);
        }

        public override int GetHashCode()
        {
            var h = 1;
            h *= 1000003;
            h ^= (int)((Timestamp >> 32) ^ Timestamp);
            h *= 1000003;
            h ^= Value.GetHashCode();
            return h;
        }
    }
}
