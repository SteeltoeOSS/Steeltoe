// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Converter;

public abstract class AbstractToCollectionConverter : AbstractGenericConditionalConverter
{
    protected readonly IConversionService ConversionService;

    protected AbstractToCollectionConverter(IConversionService conversionService)
        : base(null)
    {
        this.ConversionService = conversionService;
    }

    protected AbstractToCollectionConverter(ISet<(Type Source, Type Target)> convertableTypes, IConversionService conversionService)
        : base(convertableTypes)
    {
        this.ConversionService = conversionService;
    }
}
