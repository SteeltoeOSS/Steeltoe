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

namespace Steeltoe.Common.Util
{
    public static class ClassUtils
    {
        public static bool IsAssignableValue(Type type, object value)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return value != null ? IsAssignable(type, value.GetType()) : !type.IsPrimitive;
        }

        public static bool IsAssignable(Type lhsType, Type rhsType)
        {
            if (lhsType == null)
            {
                throw new ArgumentNullException(nameof(lhsType));
            }

            if (rhsType == null)
            {
                throw new ArgumentNullException(nameof(rhsType));
            }

            if (lhsType.IsAssignableFrom(rhsType))
            {
                return true;
            }

            // if (lhsType.IsPrimitive)
            // {
            //    Type resolvedPrimitive = primitiveWrapperTypeMap.get(rhsType);
            //    if (lhsType == resolvedPrimitive)
            //    {
            //        return true;
            //    }
            // }
            // else
            // {
            //    Type resolvedWrapper = primitiveTypeToWrapperMap.get(rhsType);
            //    if (resolvedWrapper != null && lhsType.isAssignableFrom(resolvedWrapper))
            //    {
            //        return true;
            //    }
            // }
            return false;
        }
    }
}
