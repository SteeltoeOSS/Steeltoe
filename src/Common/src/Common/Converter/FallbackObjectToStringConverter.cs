// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Common.Converter
{
    public class FallbackObjectToStringConverter : AbstractConverter<object, string>
    {
        public override bool Matches(Type sourceType, Type targetType)
        {
            if (sourceType == typeof(string))
            {
                return false;
            }

            return ObjectToObjectConverter.HasConversionMethodOrConstructor(sourceType, typeof(string));
        }

        public override string Convert(object source)
        {
            return source?.ToString();
        }
    }
}
