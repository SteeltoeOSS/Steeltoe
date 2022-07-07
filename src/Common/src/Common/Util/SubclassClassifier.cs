// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;

namespace Steeltoe.Common.Util;

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
