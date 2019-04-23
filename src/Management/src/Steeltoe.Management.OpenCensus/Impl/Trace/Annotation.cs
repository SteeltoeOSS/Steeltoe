using Steeltoe.Management.Census.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class Annotation : IAnnotation
    {
        private static readonly ReadOnlyDictionary<string, IAttributeValue> EMPTY_ATTRIBUTES =
                new ReadOnlyDictionary<string, IAttributeValue>(new Dictionary<string, IAttributeValue>());

        public static IAnnotation FromDescription(string description)
        {
            return new Annotation(description, EMPTY_ATTRIBUTES);
        }

        public static IAnnotation FromDescriptionAndAttributes(string description, IDictionary<string, IAttributeValue> attributes)
        {
            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            IDictionary<string, IAttributeValue> readOnly = new ReadOnlyDictionary<string, IAttributeValue>(attributes);
            return new Annotation(description, readOnly);
        }

        public string Description { get; }
        public IDictionary<string, IAttributeValue> Attributes { get; }

        internal Annotation(string description, IDictionary<string, IAttributeValue> attributes)
        {
            if (description == null)
            {
                throw new ArgumentNullException("Null description");
            }

            if (attributes == null)
            {
                throw new ArgumentNullException("Null attributes");
            }

            Description = description;
            Attributes = attributes;
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (obj is Annotation annotation)
            {
                return this.Description.Equals(annotation.Description) &&
                    this.Attributes.SequenceEqual(annotation.Attributes);
            }

            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.Description.GetHashCode();
            h *= 1000003;
            h ^= this.Attributes.GetHashCode();
            return h;
        }

        public override string ToString()
        {
            return "Annotation{"
                + "description=" + Description + ", "
                + "attributes=" + Collections.ToString(Attributes)
                + "}";
        }
    }
}
