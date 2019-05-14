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

namespace Steeltoe.Management.Census.Stats.Measures
{
    [Obsolete("Use OpenCensus project packages")]
    public class MeasureDouble : Measure, IMeasureDouble
    {
        public override string Name { get; }

        public override string Description { get; }

        public override string Unit { get; }

        internal MeasureDouble(string name, string description, string unit)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            this.Name = name;
            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            this.Description = description;
            if (unit == null)
            {
                throw new ArgumentNullException(nameof(unit));
            }

            this.Unit = unit;
        }

        public static IMeasureDouble Create(string name, string description, string unit)
        {
            if (!(StringUtil.IsPrintableString(name) && name.Length <= NAME_MAX_LENGTH))
            {
                throw new ArgumentOutOfRangeException(
                    "Name should be a ASCII string with a length no greater than "
                    + NAME_MAX_LENGTH
                    + " characters.");
            }

            return new MeasureDouble(name, description, unit);
        }

        public override M Match<M>(Func<IMeasureDouble, M> p0, Func<IMeasureLong, M> p1, Func<IMeasure, M> defaultFunction)
        {
            return p0.Invoke(this);
        }

        public override string ToString()
        {
            return "MeasureDouble{"
                + "name=" + Name + ", "
                + "description=" + Description + ", "
                + "unit=" + Unit
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is MeasureDouble)
            {
                MeasureDouble that = (MeasureDouble)o;
                return this.Name.Equals(that.Name)
                     && this.Description.Equals(that.Description)
                     && this.Unit.Equals(that.Unit);
            }

            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.Name.GetHashCode();
            h *= 1000003;
            h ^= this.Description.GetHashCode();
            h *= 1000003;
            h ^= this.Unit.GetHashCode();
            return h;
        }
    }
}
