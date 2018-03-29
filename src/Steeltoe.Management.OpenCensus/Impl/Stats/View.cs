using Steeltoe.Management.Census.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    public sealed class View : IView
    {
        public IViewName Name { get; }
        public String Description { get; }
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
        public static IView Create(IViewName name, String description, IMeasure measure,IAggregation aggregation,IList<ITagKey> columns)
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

        public override bool Equals(Object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is View)
            {
                View that = (View)o;
                return (this.Name.Equals(that.Name))
                     && (this.Description.Equals(that.Description))
                     && (this.Measure.Equals(that.Measure))
                     && (this.Aggregation.Equals(that.Aggregation))
                     && (this.Columns.SequenceEqual(that.Columns));
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
