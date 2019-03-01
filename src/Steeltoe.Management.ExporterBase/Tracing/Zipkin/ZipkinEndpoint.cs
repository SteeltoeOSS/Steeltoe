// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
