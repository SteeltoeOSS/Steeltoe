// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
