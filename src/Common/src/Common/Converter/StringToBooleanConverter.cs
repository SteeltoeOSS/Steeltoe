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
using System.Linq;

namespace Steeltoe.Common.Converter
{
    public class StringToBooleanConverter : AbstractGenericConditionalConverter
    {
        private static readonly ISet<string> _trueValues = new HashSet<string>() { "true", "on", "yes", "1" };

        private static readonly ISet<string> _falseValues = new HashSet<string>() { "false", "off", "no", "0" };

        public StringToBooleanConverter()
            : base(null)
        {
        }

        public override bool Matches(Type sourceType, Type targetType)
        {
            var targetCheck = ConversionUtils.GetNullableElementType(targetType);
            return sourceType == typeof(string) && targetCheck == typeof(bool);
        }

        public override object Convert(object source, Type sourceType, Type targetType)
        {
            if (source == null)
            {
                return null;
            }

            var value = ((string)source).Trim();
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (_trueValues.Contains(value, StringComparer.InvariantCultureIgnoreCase))
            {
                return true;
            }
            else if (_falseValues.Contains(value, StringComparer.InvariantCultureIgnoreCase))
            {
                return false;
            }
            else
            {
                throw new ArgumentException("Invalid boolean value '" + source + "'");
            }
        }
    }
}
