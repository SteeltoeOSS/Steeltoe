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

namespace Steeltoe.Management.Exporter.Tracing.Zipkin
{
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

            ZipkinAnnotation that = (ZipkinAnnotation)o;
            return (Timestamp == that.Timestamp) && Value.Equals(that.Value);
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= (int)((Timestamp >> 32) ^ Timestamp);
            h *= 1000003;
            h ^= Value.GetHashCode();
            return h;
        }
    }
}
