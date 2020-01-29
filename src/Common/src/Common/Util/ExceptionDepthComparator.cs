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

namespace Steeltoe.Common.Util
{
    public class ExceptionDepthComparator : IComparer<Type>
    {
        private readonly Type _targetException;

        public ExceptionDepthComparator(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            _targetException = exception.GetType();
        }

        public ExceptionDepthComparator(Type exceptionType)
        {
            if (exceptionType == null)
            {
                throw new ArgumentNullException(nameof(exceptionType));
            }

            _targetException = exceptionType;
        }

        public int Compare(Type o1, Type o2)
        {
            var depth1 = GetDepth(o1, _targetException, 0);
            var depth2 = GetDepth(o2, _targetException, 0);
            return depth1 - depth2;
        }

        private int GetDepth(Type declaredException, Type exceptionToMatch, int depth)
        {
            if (exceptionToMatch.Equals(declaredException))
            {
                // Found it!
                return depth;
            }

            // If we've gone as far as we can go and haven't found it...
            if (exceptionToMatch == typeof(Exception))
            {
                return int.MaxValue;
            }

            return GetDepth(declaredException, exceptionToMatch.BaseType, depth + 1);
        }
    }
}
