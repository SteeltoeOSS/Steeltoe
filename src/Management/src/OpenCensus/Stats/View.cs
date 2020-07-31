// <copyright file="View.cs" company="OpenCensus Authors">
// Copyright 2018, OpenCensus Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace OpenCensus.Stats
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using OpenCensus.Tags;

    public sealed class View : IView
    {
        internal View(IViewName name, string description, IMeasure measure, IAggregation aggregation, IList<ITagKey> columns)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Measure = measure ?? throw new ArgumentNullException(nameof(measure));
            Aggregation = aggregation ?? throw new ArgumentNullException(nameof(aggregation));
            Columns = columns ?? throw new ArgumentNullException(nameof(columns));
        }

        public IViewName Name { get; }

        public string Description { get; }

        public IMeasure Measure { get; }

        public IAggregation Aggregation { get; }

        public IList<ITagKey> Columns { get; }

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

        /// <inheritdoc/>
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

    /// <inheritdoc/>
        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is View that)
            {
                return Name.Equals(that.Name)
                     && Description.Equals(that.Description)
                     && Measure.Equals(that.Measure)
                     && Aggregation.Equals(that.Aggregation)
                     && Columns.SequenceEqual(that.Columns);
            }

            return false;
        }

    /// <inheritdoc/>
        public override int GetHashCode()
        {
            var h = 1;
            h *= 1000003;
            h ^= Name.GetHashCode();
            h *= 1000003;
            h ^= Description.GetHashCode();
            h *= 1000003;
            h ^= Measure.GetHashCode();
            h *= 1000003;
            h ^= Aggregation.GetHashCode();
            h *= 1000003;
            h ^= Columns.GetHashCode();
            h *= 1000003;
            return h;
        }
    }
}
