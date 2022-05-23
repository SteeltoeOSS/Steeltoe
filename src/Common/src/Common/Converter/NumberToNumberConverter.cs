// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Converter
{
    public class NumberToNumberConverter : AbstractToNumberConverter
    {
        public NumberToNumberConverter()
            : base(GetConvertiblePairs())
        {
        }

        private static ISet<(Type Source, Type Target)> GetConvertiblePairs()
        {
            return new HashSet<(Type Source, Type Target)>
            {
                (typeof(float), typeof(int)),
                (typeof(uint), typeof(int)),
                (typeof(ulong), typeof(int)),
                (typeof(long), typeof(int)),
                (typeof(double), typeof(int)),
                (typeof(short), typeof(int)),
                (typeof(ushort), typeof(int)),
                (typeof(decimal), typeof(int)),
                (typeof(byte), typeof(int)),
                (typeof(sbyte), typeof(int)),

                (typeof(int), typeof(float)),
                (typeof(uint), typeof(float)),
                (typeof(ulong), typeof(float)),
                (typeof(long), typeof(float)),
                (typeof(double), typeof(float)),
                (typeof(short), typeof(float)),
                (typeof(ushort), typeof(float)),
                (typeof(decimal), typeof(float)),
                (typeof(byte), typeof(float)),
                (typeof(sbyte), typeof(float)),

                (typeof(int), typeof(uint)),
                (typeof(float), typeof(uint)),
                (typeof(ulong), typeof(uint)),
                (typeof(long), typeof(uint)),
                (typeof(double), typeof(uint)),
                (typeof(short), typeof(uint)),
                (typeof(ushort), typeof(uint)),
                (typeof(decimal), typeof(uint)),
                (typeof(byte), typeof(uint)),
                (typeof(sbyte), typeof(uint)),

                (typeof(int), typeof(ulong)),
                (typeof(float), typeof(ulong)),
                (typeof(uint), typeof(ulong)),
                (typeof(long), typeof(ulong)),
                (typeof(double), typeof(ulong)),
                (typeof(short), typeof(ulong)),
                (typeof(ushort), typeof(ulong)),
                (typeof(decimal), typeof(ulong)),
                (typeof(byte), typeof(ulong)),
                (typeof(sbyte), typeof(ulong)),

                (typeof(int), typeof(long)),
                (typeof(float), typeof(long)),
                (typeof(uint), typeof(long)),
                (typeof(ulong), typeof(long)),
                (typeof(double), typeof(long)),
                (typeof(short), typeof(long)),
                (typeof(ushort), typeof(long)),
                (typeof(decimal), typeof(long)),
                (typeof(byte), typeof(long)),
                (typeof(sbyte), typeof(long)),

                (typeof(int), typeof(double)),
                (typeof(float), typeof(double)),
                (typeof(uint), typeof(double)),
                (typeof(ulong), typeof(double)),
                (typeof(long), typeof(double)),
                (typeof(short), typeof(double)),
                (typeof(ushort), typeof(double)),
                (typeof(decimal), typeof(double)),
                (typeof(byte), typeof(double)),
                (typeof(sbyte), typeof(double)),

                (typeof(int), typeof(short)),
                (typeof(float), typeof(short)),
                (typeof(uint), typeof(short)),
                (typeof(ulong), typeof(short)),
                (typeof(long), typeof(short)),
                (typeof(double), typeof(short)),
                (typeof(ushort), typeof(short)),
                (typeof(decimal), typeof(short)),
                (typeof(byte), typeof(short)),
                (typeof(sbyte), typeof(short)),

                (typeof(int), typeof(decimal)),
                (typeof(float), typeof(decimal)),
                (typeof(uint), typeof(decimal)),
                (typeof(ulong), typeof(decimal)),
                (typeof(long), typeof(decimal)),
                (typeof(double), typeof(decimal)),
                (typeof(short), typeof(decimal)),
                (typeof(ushort), typeof(decimal)),
                (typeof(byte), typeof(decimal)),
                (typeof(sbyte), typeof(decimal)),

                (typeof(int), typeof(byte)),
                (typeof(float), typeof(byte)),
                (typeof(uint), typeof(byte)),
                (typeof(ulong), typeof(byte)),
                (typeof(long), typeof(byte)),
                (typeof(double), typeof(byte)),
                (typeof(short), typeof(byte)),
                (typeof(ushort), typeof(byte)),
                (typeof(decimal), typeof(byte)),
                (typeof(sbyte), typeof(byte)),
            };
        }
    }
}
