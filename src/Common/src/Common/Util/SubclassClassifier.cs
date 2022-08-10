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

        Type type = classifiable.GetType();

        if (TypeMap.TryGetValue(type, out TTarget result))
        {
            return result;
        }

        // check for subclasses
        bool foundValue = false;
        var value = default(TTarget);

        for (Type currentType = type; !currentType.Equals(typeof(object)); currentType = currentType.BaseType)
        {
            if (TypeMap.TryGetValue(currentType, out value))
            {
                foundValue = true;
                break;
            }
        }

        if (foundValue)
        {
            TypeMap.TryAdd(type, value);
            return value;
        }

        return DefaultValue;
    }
}
