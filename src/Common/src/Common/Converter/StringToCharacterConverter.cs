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

namespace Steeltoe.Common.Converter
{
    public class StringToCharacterConverter : AbstractGenericConditionalConverter
    {
        public StringToCharacterConverter()
            : base(null)
        {
        }

        public override bool Matches(Type sourceType, Type targetType)
        {
            var targetCheck = ConversionUtils.GetNullableElementType(targetType);
            return sourceType == typeof(string) && targetCheck == typeof(char);
        }

        public override object Convert(object source, Type sourceType, Type targetType)
        {
            var asString = source as string;
            if (string.IsNullOrEmpty(asString))
            {
                return null;
            }

            if (asString.Length > 1)
            {
                throw new ArgumentException("Can only convert a [String] with length of 1 to a [Character]; string value '" + source + "'  has length of " + asString.Length);
            }

            return asString[0];
        }
    }
}
