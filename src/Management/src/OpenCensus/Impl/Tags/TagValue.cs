// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Management.Census.Utils;
using System;

namespace Steeltoe.Management.Census.Tags
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class TagValue : ITagValue
    {
        public const int MAX_LENGTH = 255;

        public string AsString { get; }

        internal TagValue(string asString)
        {
            if (asString == null)
            {
                throw new ArgumentNullException(nameof(asString));
            }

            this.AsString = asString;
        }

        public static ITagValue Create(string value)
        {
            if (!IsValid(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return new TagValue(value);
        }

        public override string ToString()
        {
            return "TagValue{"
                + "asString=" + AsString
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is TagValue)
            {
                TagValue that = (TagValue)o;
                return this.AsString.Equals(that.AsString);
            }

            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.AsString.GetHashCode();
            return h;
        }

        private static bool IsValid(string value)
        {
            return value.Length <= MAX_LENGTH && StringUtil.IsPrintableString(value);
        }
    }
}
