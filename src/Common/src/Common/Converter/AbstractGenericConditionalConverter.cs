// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Converter;

public abstract class AbstractGenericConditionalConverter : AbstractGenericConverter, IConditionalGenericConverter
{
    protected AbstractGenericConditionalConverter(ISet<(Type Source, Type Target)> convertableTypes)
        : base(convertableTypes)
    {
    }

    public abstract bool Matches(Type sourceType, Type targetType);
}
