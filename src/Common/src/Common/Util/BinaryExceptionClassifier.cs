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
using System.Collections.Generic;

namespace Steeltoe.Common.Util
{
    public class BinaryExceptionClassifier : SubclassClassifier<Exception, bool>
    {
        public bool TraverseInnerExceptions { get; set; } = true;

        public BinaryExceptionClassifier(bool defaultValue)
         : base(defaultValue)
        {
        }

        public BinaryExceptionClassifier(IList<Type> exceptionClasses, bool defaultValue)
        : this(!defaultValue)
        {
            if (exceptionClasses != null)
            {
                var map = new ConcurrentDictionary<Type, bool>();
                foreach (var type in exceptionClasses)
                {
                    map.TryAdd(type, !DefaultValue);
                }

                TypeMap = map;
            }
        }

        public BinaryExceptionClassifier(IList<Type> exceptionClasses)
        : this(exceptionClasses, true)
        {
        }

        public BinaryExceptionClassifier(Dictionary<Type, bool> typeMap)
        : this(typeMap, false)
        {
        }

        public BinaryExceptionClassifier(Dictionary<Type, bool> typeMap, bool defaultValue)
        : base(new ConcurrentDictionary<Type, bool>(typeMap), defaultValue)
        {
        }

        public override bool Classify(Exception classifiable)
        {
            var classified = base.Classify(classifiable);
            if (!TraverseInnerExceptions)
            {
                return classified;
            }

            if (classified == DefaultValue)
            {
                var cause = classifiable;
                do
                {
                    if (TypeMap.TryGetValue(cause.GetType(), out classified))
                    {
                        return classified;
                    }

                    cause = cause.InnerException;
                    classified = base.Classify(cause);
                }
                while (cause != null && classified == DefaultValue);
            }

            return classified;
        }
    }
}
