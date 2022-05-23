// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Converter
{
    public class NumberToStringConverter : AbstractGenericConverter
    {
        public NumberToStringConverter()
            : base(GetConvertiblePairs())
        {
        }

        public override object Convert(object source, Type sourceType, Type targetType)
        {
            return System.Convert.ToString(source);
        }

        private static ISet<(Type Source, Type Target)> GetConvertiblePairs()
        {
            return new HashSet<(Type Source, Type Target)>
            {
                (typeof(int), typeof(string)),
                (typeof(float), typeof(string)),
                (typeof(uint), typeof(string)),
                (typeof(ulong), typeof(string)),
                (typeof(long), typeof(string)),
                (typeof(double), typeof(string)),
                (typeof(short), typeof(string)),
                (typeof(ushort), typeof(string)),
                (typeof(decimal), typeof(string)),
                (typeof(byte), typeof(string)),
                (typeof(sbyte), typeof(string)),
            };
        }
    }
}
