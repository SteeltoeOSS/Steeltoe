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

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public abstract class AttributeValue : IAttributeValue
    {
        public static IAttributeValue<string> StringAttributeValue(string stringValue)
        {
            if (stringValue == null)
            {
                throw new ArgumentNullException(nameof(stringValue));
            }

            return new AttributeValue<string>(stringValue);
        }

        public static IAttributeValue<long> LongAttributeValue(long longValue)
        {
            return new AttributeValue<long>(longValue);
        }

        public static IAttributeValue<bool> BooleanAttributeValue(bool booleanValue)
        {
            return new AttributeValue<bool>(booleanValue);
        }

        internal AttributeValue()
        {
        }

        public abstract T Match<T>(
            Func<string, T> stringFunction,
            Func<bool, T> booleanFunction,
            Func<long, T> longFunction,
            Func<object, T> defaultFunction);
    }

    [Obsolete("Use OpenCensus project packages")]
#pragma warning disable SA1402 // File may only contain a single class
    public sealed class AttributeValue<T> : AttributeValue, IAttributeValue<T>
#pragma warning restore SA1402 // File may only contain a single class
    {
        public static IAttributeValue<string> Create(string stringValue)
        {
            if (stringValue == null)
            {
                throw new ArgumentNullException(nameof(stringValue));
            }

            return new AttributeValue<string>(stringValue);
        }

        public static IAttributeValue<long> Create(long longValue)
        {
            return new AttributeValue<long>(longValue);
        }

        public static IAttributeValue<bool> Create(bool booleanValue)
        {
            return new AttributeValue<bool>(booleanValue);
        }

        public T Value { get; }

        internal AttributeValue(T value)
        {
            Value = value;
        }

        public M Apply<M>(Func<T, M> function)
        {
            return function(Value);
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (obj is AttributeValue<T> attribute)
            {
                return attribute.Value.Equals(this.Value);
            }

            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= Value.GetHashCode();
            return h;
        }

        public override string ToString()
        {
            return "AttributeValue{"
                + "Value=" + Value.ToString()
                + "}";
        }

        public override M Match<M>(
            Func<string, M> stringFunction,
            Func<bool, M> booleanFunction,
            Func<long, M> longFunction,
            Func<object, M> defaultFunction)
        {
            if (typeof(T) == typeof(string))
            {
                string value = Value as string;
                return stringFunction(value);
            }

            if (typeof(T) == typeof(long))
            {
                long val = (long)(object)Value;
                return longFunction(val);
            }

            if (typeof(T) == typeof(bool))
            {
                bool val = (bool)(object)Value;
                return booleanFunction(val);
            }

            return defaultFunction(Value);
        }
    }
}
