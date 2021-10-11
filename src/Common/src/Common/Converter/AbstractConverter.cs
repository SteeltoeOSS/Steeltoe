// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Converter
{
    public abstract class AbstractConverter<S, T> : AbstractGenericConditionalConverter, IConverter<S, T>
    {
        protected AbstractConverter()
            : base(new HashSet<(Type Source, Type Target)>() { (typeof(S), typeof(T)) })
        {
        }

        public override bool Matches(Type sourceType, Type targetType) => typeof(T) == targetType;

        public abstract T Convert(S source);

        public override object Convert(object source, Type sourceType, Type targetType)
            => source switch
                {
                    null => null,
                    not S => throw new ArgumentException("'source' type invalid"),
                    _ => Convert((S)source)
                };
    }
}
