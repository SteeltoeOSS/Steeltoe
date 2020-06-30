// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
