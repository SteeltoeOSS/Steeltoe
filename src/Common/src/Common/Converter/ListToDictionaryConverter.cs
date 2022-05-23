// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Steeltoe.Common.Converter
{
    public class ListToDictionaryConverter : CollectionToObjectConverter
    {
        public ListToDictionaryConverter(IConversionService conversionService)
            : base(conversionService, new HashSet<(Type Source, Type Target)> { (typeof(IList<object>), typeof(IDictionary<string, object>)) })
        {
        }
    }
}
