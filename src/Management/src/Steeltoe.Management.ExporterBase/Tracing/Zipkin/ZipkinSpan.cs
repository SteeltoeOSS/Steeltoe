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
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Exporter.Tracing.Zipkin
{
    [Obsolete("Use OpenCensus project packages")]
    internal class ZipkinSpan
    {
        internal const int FLAG_DEBUG = 1 << 1;
        internal const int FLAG_DEBUG_SET = 1 << 2;
        internal const int FLAG_SHARED = 1 << 3;
        internal const int FLAG_SHARED_SET = 1 << 4;

        [JsonProperty("traceId")]
        public string TraceId { get; set; }

        [JsonProperty("parentId")]
        public string ParentId { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("kind")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ZipkinSpanKind Kind { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("duration")]
        public long Duration { get; set; }

        [JsonProperty("localEndpoint")]
        public ZipkinEndpoint LocalEndpoint { get; set; }

        [JsonProperty("remoteEndpoint")]
        public ZipkinEndpoint RemoteEndpoint { get; set; }

        [JsonProperty("annotations")]
        public List<ZipkinAnnotation> Annotations { get; set; }

        [JsonProperty("tags")]
        public Dictionary<string, string> Tags { get; set; }

        [JsonProperty("debug")]
        public bool Debug { get; set; }

        [JsonProperty("shared")]
        public bool Shared { get; set; }

        public static Builder NewBuilder()
        {
            return new Builder();
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (!(o is ZipkinSpan))
            {
                return false;
            }

            ZipkinSpan that = (ZipkinSpan)o;
            return TraceId.Equals(that.TraceId)
              && ((ParentId == null) ? (that.ParentId == null) : ParentId.Equals(that.ParentId))
              && Id.Equals(that.Id)
              && Kind.Equals(that.Kind)
              && ((Name == null) ? (that.Name == null) : Name.Equals(that.Name))
              && (Timestamp == that.Timestamp)
              && (Duration == that.Duration)
              && ((LocalEndpoint == null) ? (that.LocalEndpoint == null) : LocalEndpoint.Equals(that.LocalEndpoint))
              && ((RemoteEndpoint == null) ? (that.RemoteEndpoint == null) : RemoteEndpoint.Equals(that.RemoteEndpoint))
              && Annotations.SequenceEqual(that.Annotations)
              && Tags.SequenceEqual(that.Tags)
              && (Debug == that.Debug)
              && (Shared == that.Shared);
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= TraceId.GetHashCode();
            h *= 1000003;
            h ^= (ParentId == null) ? 0 : ParentId.GetHashCode();
            h *= 1000003;
            h ^= Id.GetHashCode();
            h *= 1000003;
            h ^= Kind.GetHashCode();
            h *= 1000003;
            h ^= (Name == null) ? 0 : Name.GetHashCode();
            h *= 1000003;
            h ^= (int)(h ^ ((Timestamp >> 32) ^ Timestamp));
            h *= 1000003;
            h ^= (int)(h ^ ((Duration >> 32) ^ Duration));
            h *= 1000003;
            h ^= (LocalEndpoint == null) ? 0 : LocalEndpoint.GetHashCode();
            h *= 1000003;
            h ^= (RemoteEndpoint == null) ? 0 : RemoteEndpoint.GetHashCode();
            h *= 1000003;
            h ^= Annotations.GetHashCode();
            h *= 1000003;
            h ^= Tags.GetHashCode();
            h *= 1000003;
            return h;
        }

        public class Builder
        {
            private string traceId;
            private string parentId;
            private string id;
            private ZipkinSpanKind kind;
            private string name;
            private long timestamp;
            private long duration; // zero means null
            private ZipkinEndpoint localEndpoint;
            private ZipkinEndpoint remoteEndpoint;
            private List<ZipkinAnnotation> annotations;
            private Dictionary<string, string> tags;
            private bool debug;
            private bool shared;

            internal Builder TraceId(string v)
            {
                traceId = v;
                return this;
            }

            internal Builder Id(string v)
            {
                id = v;
                return this;
            }

            internal Builder ParentId(string v)
            {
                parentId = v;
                return this;
            }

            internal Builder Kind(ZipkinSpanKind v)
            {
                kind = v;
                return this;
            }

            internal Builder Name(string v)
            {
                name = v;
                return this;
            }

            internal Builder Timestamp(long v)
            {
                timestamp = v;
                return this;
            }

            internal Builder Duration(long v)
            {
                duration = v;
                return this;
            }

            internal Builder LocalEndpoint(ZipkinEndpoint v)
            {
                localEndpoint = v;
                return this;
            }

            internal Builder RemoteEndpoint(ZipkinEndpoint v)
            {
                remoteEndpoint = v;
                return this;
            }

            internal Builder Debug(bool v)
            {
                debug = v;
                return this;
            }

            internal Builder Shared(bool v)
            {
                shared = v;
                return this;
            }

            internal Builder PutTag(string key, string value)
            {
                if (tags == null)
                {
                    tags = new Dictionary<string, string>();
                }

                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                this.tags[key] = value;
                return this;
            }

            internal Builder AddAnnotation(long timestamp, string value)
            {
                if (annotations == null)
                {
                    annotations = new List<ZipkinAnnotation>(2);
                }

                annotations.Add(new ZipkinAnnotation() { Timestamp = timestamp, Value = value });
                return this;
            }

            internal ZipkinSpan Build()
            {
                string missing = string.Empty;
                if (traceId == null)
                {
                    missing += " traceId";
                }

                if (id == null)
                {
                    missing += " id";
                }

                if (!string.Empty.Equals(missing))
                {
                    throw new ArgumentException("Missing :" + missing);
                }

                return new ZipkinSpan()
                {
                    TraceId = traceId,
                    ParentId = parentId,
                    Id = id,
                    Kind = kind,
                    Name = name,
                    Timestamp = timestamp,
                    Duration = duration,
                    LocalEndpoint = localEndpoint,
                    RemoteEndpoint = remoteEndpoint,
                    Annotations = annotations,
                    Tags = tags,
                    Shared = shared,
                    Debug = debug
                };
            }
        }
    }
}
