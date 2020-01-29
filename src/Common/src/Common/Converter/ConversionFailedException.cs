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
    public class ConversionFailedException : ConversionException
    {
        public ConversionFailedException(Type sourceType, Type targetType, object value, Exception cause)
            : base("Failed to convert from type [" + sourceType + "] to type [" + targetType + "] for value '" + (value == null ? "null" : value.ToString()) + "'", cause)
        {
            SourceType = sourceType;
            TargetType = targetType;
            Value = value;
        }

        public Type SourceType { get; }

        public Type TargetType { get; }

        public object Value { get; }
    }
}
