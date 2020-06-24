// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            private string _traceId;
            private string _parentId;
            private string _id;
            private ZipkinSpanKind _kind;
            private string _name;
            private long _timestamp;
            private long _duration; // zero means null
            private ZipkinEndpoint _localEndpoint;
            private ZipkinEndpoint _remoteEndpoint;
            private List<ZipkinAnnotation> _annotations;
            private Dictionary<string, string> _tags;
            private bool _debug;
            private bool _shared;

            internal Builder TraceId(string v)
            {
                _traceId = v;
                return this;
            }

            internal Builder Id(string v)
            {
                _id = v;
                return this;
            }

            internal Builder ParentId(string v)
            {
                _parentId = v;
                return this;
            }

            internal Builder Kind(ZipkinSpanKind v)
            {
                _kind = v;
                return this;
            }

            internal Builder Name(string v)
            {
                _name = v;
                return this;
            }

            internal Builder Timestamp(long v)
            {
                _timestamp = v;
                return this;
            }

            internal Builder Duration(long v)
            {
                _duration = v;
                return this;
            }

            internal Builder LocalEndpoint(ZipkinEndpoint v)
            {
                _localEndpoint = v;
                return this;
            }

            internal Builder RemoteEndpoint(ZipkinEndpoint v)
            {
                _remoteEndpoint = v;
                return this;
            }

            internal Builder Debug(bool v)
            {
                _debug = v;
                return this;
            }

            internal Builder Shared(bool v)
            {
                _shared = v;
                return this;
            }

            internal Builder PutTag(string key, string value)
            {
                if (_tags == null)
                {
                    _tags = new Dictionary<string, string>();
                }

                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                this._tags[key] = value;
                return this;
            }

            internal Builder AddAnnotation(long timestamp, string value)
            {
                if (_annotations == null)
                {
                    _annotations = new List<ZipkinAnnotation>(2);
                }

                _annotations.Add(new ZipkinAnnotation() { Timestamp = timestamp, Value = value });
                return this;
            }

            internal ZipkinSpan Build()
            {
                string missing = string.Empty;
                if (_traceId == null)
                {
                    missing += " traceId";
                }

                if (_id == null)
                {
                    missing += " id";
                }

                if (!string.IsNullOrEmpty(missing))
                {
                    throw new ArgumentException("Missing :" + missing);
                }

                return new ZipkinSpan()
                {
                    TraceId = _traceId,
                    ParentId = _parentId,
                    Id = _id,
                    Kind = _kind,
                    Name = _name,
                    Timestamp = _timestamp,
                    Duration = _duration,
                    LocalEndpoint = _localEndpoint,
                    RemoteEndpoint = _remoteEndpoint,
                    Annotations = _annotations,
                    Tags = _tags,
                    Shared = _shared,
                    Debug = _debug
                };
            }
        }
    }
}
