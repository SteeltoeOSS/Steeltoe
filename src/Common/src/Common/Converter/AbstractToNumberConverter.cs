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
    public abstract class AbstractToNumberConverter : AbstractGenericConditionalConverter
    {
        protected ISet<(Type Source, Type Target)> _convertableTypes;

        protected AbstractToNumberConverter(ISet<(Type Source, Type Target)> convertableTypes)
            : base(null)
        {
            _convertableTypes = convertableTypes;
        }

        public override bool Matches(Type sourceType, Type targetType)
        {
            var targetCheck = ConversionUtils.GetNullableElementType(targetType);
            var pair = (sourceType, targetCheck);
            return _convertableTypes.Contains(pair);
        }

        public override object Convert(object source, Type sourceType, Type targetType)
        {
            targetType = ConversionUtils.GetNullableElementType(targetType);
            if (typeof(int) == targetType)
            {
                return System.Convert.ToInt32(source);
            }
            else if (typeof(float) == targetType)
            {
                return System.Convert.ToSingle(source);
            }
            else if (typeof(uint) == targetType)
            {
                return System.Convert.ToUInt32(source);
            }
            else if (typeof(ulong) == targetType)
            {
                return System.Convert.ToUInt64(source);
            }
            else if (typeof(long) == targetType)
            {
                return System.Convert.ToInt64(source);
            }
            else if (typeof(double) == targetType)
            {
                return System.Convert.ToDouble(source);
            }
            else if (typeof(short) == targetType)
            {
                return System.Convert.ToInt16(source);
            }
            else if (typeof(ushort) == targetType)
            {
                return System.Convert.ToUInt16(source);
            }
            else if (typeof(decimal) == targetType)
            {
                return System.Convert.ToDecimal(source);
            }
            else if (typeof(byte) == targetType)
            {
                return System.Convert.ToByte(source);
            }
            else if (typeof(sbyte) == targetType)
            {
                return System.Convert.ToSByte(source);
            }

            throw new ArgumentException(nameof(targetType));
        }
    }
}
