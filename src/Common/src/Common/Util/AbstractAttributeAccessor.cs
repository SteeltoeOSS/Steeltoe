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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Common.Util
{
    public abstract class AbstractAttributeAccessor : IAttributeAccessor
    {
        private readonly Dictionary<string, object> _attributes = new Dictionary<string, object>();

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

            if (!(other is AbstractAttributeAccessor))
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
