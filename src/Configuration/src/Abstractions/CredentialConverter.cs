// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Globalization;

namespace Steeltoe.Extensions.Configuration;

public class CredentialConverter : TypeConverter
{
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        => value is string stringVal
            ? new Credential(stringVal)
            : base.ConvertFrom(context, culture, value);

    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
}
