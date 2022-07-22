// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Converter;

public abstract class AbstractGenericConverter : IGenericConverter
{
    protected AbstractGenericConverter(ISet<(Type Source, Type Target)> convertableTypes)
    {
        ConvertibleTypes = convertableTypes;
    }

    public ISet<(Type Source, Type Target)> ConvertibleTypes { get; }

    public abstract object Convert(object source, Type sourceType, Type targetType);
}