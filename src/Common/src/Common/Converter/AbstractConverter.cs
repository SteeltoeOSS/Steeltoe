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

        public override bool Matches(Type sourceType, Type targetType)
        {
            if (typeof(T) != targetType)
            {
                return false;
            }

            return true;
        }

        public abstract T Convert(S source);

        public override object Convert(object source, Type sourceType, Type targetType)
        {
            if (source == null)
            {
                return null;
            }

            if (!typeof(S).IsAssignableFrom(source.GetType()))
            {
                throw new ArgumentException("'source' type invalid");
            }

            return Convert((S)source);
        }
    }
}
