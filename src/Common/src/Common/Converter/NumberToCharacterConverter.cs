// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Converter
{
    public class NumberToCharacterConverter : AbstractGenericConverter
    {
        public NumberToCharacterConverter()
        : base(GetConvertiblePairs())
        {
        }

        public override object Convert(object source, Type sourceType, Type targetType)
        {
            return System.Convert.ToChar(source);
        }

        private static ISet<(Type Source, Type Target)> GetConvertiblePairs()
        {
            return new HashSet<(Type Source, Type Target)>()
            {
                (typeof(int), typeof(char)),
                (typeof(uint), typeof(char)),
                (typeof(ulong), typeof(char)),
                (typeof(long), typeof(char)),
                (typeof(short), typeof(char)),
                (typeof(ushort), typeof(char)),
            };
        }
    }
}
