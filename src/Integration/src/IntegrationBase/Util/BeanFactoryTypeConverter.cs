// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Converter;
using Steeltoe.Common.Expression.Internal;
using System;

namespace Steeltoe.Integration.Util
{
    public class BeanFactoryTypeConverter : ITypeConverter
    {
        public BeanFactoryTypeConverter()
        {
            ConversionService = DefaultConversionService.Singleton;
        }

        public BeanFactoryTypeConverter(IConversionService conversionService)
        {
            ConversionService = conversionService ?? throw new ArgumentNullException(nameof(conversionService));
        }

        public IConversionService ConversionService { get; set; }

        public bool CanConvert(Type source, Type target)
        {
            return ConversionService.CanConvert(source, target);

            // else if( !typeof(string).IsAssignableFrom(source) && !typeof(string).IsAssignableFrom(target))
            // {
            //    return false;
            // }
            // else if ( !typeof(string).IsAssignableFrom(source))
            // {
            //    return this.de // no SimpleTypeConverter ....
            // }
        }

        public object ConvertValue(object value, Type source, Type target)
        {
            if (CanConvert(source, target))
            {
                return ConversionService.Convert(value, source, target);
            }
            else
            {
                throw new NotImplementedException("Cannot be converted by default service");
            }
        }
    }
}
