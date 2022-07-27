// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Common.Converter;

public class StringToGuidConverter : AbstractConverter<string, Guid>
{
    public override Guid Convert(string source)
    {
        return Guid.Parse(source.Trim());
    }

    public override object Convert(object source, Type sourceType, Type targetType)
    {
        return Convert((string)source);
    }
}