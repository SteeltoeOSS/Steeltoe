﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Converter
{
    public class CharacterToNumberConverter : AbstractToNumberConverter
    {
        public CharacterToNumberConverter()
            : base(GetConvertiblePairs())
        {
        }

        private static ISet<(Type Source, Type Target)> GetConvertiblePairs()
        {
            return new HashSet<(Type Source, Type Target)>()
            {
                (typeof(char), typeof(int)),
                (typeof(char), typeof(float)),
                (typeof(char), typeof(uint)),
                (typeof(char), typeof(ulong)),
                (typeof(char), typeof(long)),
                (typeof(char), typeof(double)),
                (typeof(char), typeof(short)),
                (typeof(char), typeof(ushort)),
                (typeof(char), typeof(decimal)),
                (typeof(char), typeof(byte)),
                (typeof(char), typeof(sbyte)),
            };
        }
    }
}
