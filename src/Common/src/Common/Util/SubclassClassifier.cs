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
using System.Collections.Concurrent;

namespace Steeltoe.Common.Util
{
    public class SubclassClassifier<T, C> : IClassifier<T, C>
    {
        public C DefaultValue { get; set; }

        protected ConcurrentDictionary<Type, C> TypeMap { get; set; }

        public SubclassClassifier()
        : this(default)
        {
        }

        public SubclassClassifier(C defaultValue)
        : this(new ConcurrentDictionary<Type, C>(), defaultValue)
        {
        }

        public SubclassClassifier(ConcurrentDictionary<Type, C> typeMap, C defaultValue)
            : base()
        {
            TypeMap = new ConcurrentDictionary<Type, C>(typeMap);
            DefaultValue = defaultValue;
        }

        public virtual C Classify(T classifiable)
        {
            if (classifiable == null)
            {
                return DefaultValue;
            }

            var clazz = classifiable.GetType();

            if (TypeMap.TryGetValue(clazz, out var result))
            {
                return result;
            }

            // check for subclasses
            var foundValue = false;
            var value = default(C);
            for (var cls = clazz; !cls.Equals(typeof(object)); cls = cls.BaseType)
            {
                if (TypeMap.TryGetValue(cls, out value))
                {
                    foundValue = true;
                    break;
                }
            }

            if (foundValue)
            {
                TypeMap.TryAdd(clazz, value);
                return value;
            }

            return DefaultValue;
        }
    }
}
