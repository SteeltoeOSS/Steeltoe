// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace Steeltoe.Common.Util;

public class SubclassClassifier<TSource, TTarget> : IClassifier<TSource, TTarget>
{
    protected ConcurrentDictionary<Type, TTarget> TypeMap { get; set; }
    public TTarget DefaultValue { get; set; }

    public SubclassClassifier()
        : this(default)
    {
    }

    public SubclassClassifier(TTarget defaultValue)
        : this(new ConcurrentDictionary<Type, TTarget>(), defaultValue)
    {
    }

    public SubclassClassifier(ConcurrentDictionary<Type, TTarget> typeMap, TTarget defaultValue)
    {
        TypeMap = new ConcurrentDictionary<Type, TTarget>(typeMap);
        DefaultValue = defaultValue;
    }

    public virtual TTarget Classify(TSource classifiable)
    {
        if (classifiable == null)
        {
            return DefaultValue;
        }

        Type clazz = classifiable.GetType();

        if (TypeMap.TryGetValue(clazz, out TTarget result))
        {
            return result;
        }

        // check for subclasses
        bool foundValue = false;
        var value = default(TTarget);

        for (Type cls = clazz; !cls.Equals(typeof(object)); cls = cls.BaseType)
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
