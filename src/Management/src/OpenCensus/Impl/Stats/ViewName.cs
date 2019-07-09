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

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class ViewName : IViewName
    {
        internal const int NAME_MAX_LENGTH = 255;

        public string AsString { get; }

        internal ViewName(string asString)
        {
            if (asString == null)
            {
                throw new ArgumentNullException(nameof(asString));
            }

            this.AsString = asString;
        }

        public static IViewName Create(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!(StringUtil.IsPrintableString(name) && name.Length <= NAME_MAX_LENGTH))
            {
                throw new ArgumentOutOfRangeException(
                    "Name should be a ASCII string with a length no greater than "
                    + NAME_MAX_LENGTH
                    + " characters.");
            }

            return new ViewName(name);
        }

        public override string ToString()
        {
            return "Name{"
                + "asString=" + AsString
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is ViewName)
            {
                ViewName that = (ViewName)o;
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
    }
}
