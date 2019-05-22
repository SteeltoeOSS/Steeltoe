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

using Steeltoe.Management.Census.Tags;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class View : IView
    {
        public IViewName Name { get; }

        public string Description { get; }

        public IMeasure Measure { get; }

        public IAggregation Aggregation { get; }

        public IList<ITagKey> Columns { get; }

        internal View(IViewName name, string description, IMeasure measure, IAggregation aggregation, IList<ITagKey> columns)
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
            if (measure == null)
            {
                throw new ArgumentNullException(nameof(measure));
            }

            this.Measure = measure;
            if (aggregation == null)
            {
                throw new ArgumentNullException(nameof(aggregation));
            }

            this.Aggregation = aggregation;
            if (columns == null)
            {
                throw new ArgumentNullException(nameof(columns));
            }

            this.Columns = columns;
        }

        public static IView Create(IViewName name, string description, IMeasure measure, IAggregation aggregation, IList<ITagKey> columns)
        {
            var set = new HashSet<ITagKey>(columns);
            if (set.Count != columns.Count)
            {
                throw new ArgumentException("Columns have duplicate.");
            }

            return new View(
                name,
                description,
                measure,
                aggregation,
                new List<ITagKey>(columns).AsReadOnly());
        }

        public override string ToString()
        {
            return "View{"
                + "name=" + Name + ", "
                + "description=" + Description + ", "
                + "measure=" + Measure + ", "
                + "aggregation=" + Aggregation + ", "
                + "columns=" + Columns + ", "
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is View)
            {
                View that = (View)o;
                return this.Name.Equals(that.Name)
                     && this.Description.Equals(that.Description)
                     && this.Measure.Equals(that.Measure)
                     && this.Aggregation.Equals(that.Aggregation)
                     && this.Columns.SequenceEqual(that.Columns);
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
            h ^= this.Measure.GetHashCode();
            h *= 1000003;
            h ^= this.Aggregation.GetHashCode();
            h *= 1000003;
            h ^= this.Columns.GetHashCode();
            h *= 1000003;
            return h;
        }
    }
}
