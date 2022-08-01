// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace Steeltoe.Common.Util;

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
