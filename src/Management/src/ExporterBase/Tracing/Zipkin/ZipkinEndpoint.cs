// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using System;

namespace Steeltoe.Management.Exporter.Tracing.Zipkin
{
    [Obsolete("Use OpenCensus project packages")]
    internal class ZipkinEndpoint
    {
        [JsonProperty("serviceName")]
        public string ServiceName { get; set; }

        [JsonProperty("ipv4")]
        public string Ipv4 { get; set; }

        [JsonProperty("ipv6")]
        public string Ipv6 { get; set; }

        [JsonProperty("port")]
        public int Port { get; set; }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (!(o is ZipkinEndpoint))
            {
                return false;
            }

            ZipkinEndpoint that = (ZipkinEndpoint)o;
            return ((ServiceName == null)
              ? (that.ServiceName == null) : ServiceName.Equals(that.ServiceName))
              && ((Ipv4 == null) ? (that.Ipv4 == null) : Ipv4.Equals(that.Ipv4))
              && ((Ipv6 == null) ? (that.Ipv6 == null) : Ipv6.Equals(that.Ipv6))
              && Port.Equals(that.Port);
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= (ServiceName == null) ? 0 : ServiceName.GetHashCode();
            h *= 1000003;
            h ^= (Ipv4 == null) ? 0 : Ipv4.GetHashCode();
            h *= 1000003;
            h ^= (Ipv6 == null) ? 0 : Ipv6.GetHashCode();
            h *= 1000003;
            h ^= Port.GetHashCode();
            return h;
        }
    }
}
