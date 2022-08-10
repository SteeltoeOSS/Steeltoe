// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Converter;

public abstract class AbstractGenericConverter : IGenericConverter
{
    public ISet<(Type SourceType, Type TargetType)> ConvertibleTypes { get; }

    protected AbstractGenericConverter(ISet<(Type SourceType, Type TargetType)> convertableTypes)
    {
        ConvertibleTypes = convertableTypes;
    }

    public abstract object Convert(object source, Type sourceType, Type targetType);
}
