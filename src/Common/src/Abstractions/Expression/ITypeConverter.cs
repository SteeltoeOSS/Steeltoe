﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Common.Expression
{
    /// <summary>
    /// A type converter can convert values between different types encountered during expression
    /// evaluation.
    /// TODO:  This interface is not complete
    /// </summary>
    public interface ITypeConverter
    {
        bool CanConvert(Type source, Type target);

        object ConvertValue(object value, Type source, Type target);
    }
}
