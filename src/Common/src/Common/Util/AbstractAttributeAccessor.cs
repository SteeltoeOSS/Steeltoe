// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Common.Util
{
    public abstract class AbstractAttributeAccessor : IAttributeAccessor
    {
        private readonly Dictionary<string, object> _attributes = new ();

        public virtual void SetAttribute(string name, object value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (value != null)
            {
                _attributes[name] = value;
            }
            else
            {
                RemoveAttribute(name);
            }
        }

        public virtual object GetAttribute(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            _attributes.TryGetValue(name, out var result);
            return result;
        }

        public virtual object RemoveAttribute(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            _attributes.TryGetValue(name, out var original);
            _attributes.Remove(name);
            return original;
        }

        public virtual bool HasAttribute(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return _attributes.ContainsKey(name);
        }

        public virtual string[] AttributeNames
        {
#pragma warning disable S2365 // Properties should not make collection or array copies
            get { return _attributes.Keys.ToArray(); }
#pragma warning restore S2365 // Properties should not make collection or array copies
        }

        public override bool Equals(object other)
        {
            if (this == other)
            {
                return true;
            }

            if (other is not AbstractAttributeAccessor)
            {
                return false;
            }

            var accessor = (AbstractAttributeAccessor)other;
            if (accessor._attributes.Count != _attributes.Count)
            {
                return false;
            }

            foreach (var kvp in _attributes)
            {
                object value2;
                if (!accessor._attributes.TryGetValue(kvp.Key, out value2))
                {
                    return false;
                }

                if (!kvp.Value.Equals(value2))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            return _attributes.GetHashCode();
        }

        protected virtual void CopyAttributesFrom(IAttributeAccessor source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var attributeNames = source.AttributeNames;
            foreach (var attributeName in attributeNames)
            {
                SetAttribute(attributeName, source.GetAttribute(attributeName));
            }
        }
    }
}
