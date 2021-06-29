// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public static class AstUtils
    {
        public static List<IPropertyAccessor> GetPropertyAccessorsToTry(Type targetType, List<IPropertyAccessor> propertyAccessors)
        {
            var specificAccessors = new List<IPropertyAccessor>();
            var generalAccessors = new List<IPropertyAccessor>();
            foreach (var resolver in propertyAccessors)
            {
                var targets = resolver.GetSpecificTargetClasses();
                if (targets == null)
                {
                    // generic resolver that says it can be used for any type
                    generalAccessors.Add(resolver);
                }
                else
                {
                    if (targetType != null)
                    {
                        var pos = 0;
                        foreach (var clazz in targets)
                        {
                            if (clazz == targetType)
                            {
                                // put exact matches on the front to be tried first?
                                specificAccessors.Insert(pos++, resolver);
                            }
                            else if (clazz.IsAssignableFrom(targetType))
                            {
                                // put supertype matches at the end of the
                                // specificAccessor list
                                generalAccessors.Add(resolver);
                            }
                        }
                    }
                }
            }

            var resolvers = new List<IPropertyAccessor>(specificAccessors.Count + generalAccessors.Count);
            resolvers.AddRange(specificAccessors);
            resolvers.AddRange(generalAccessors);
            return resolvers;
        }
    }
}
