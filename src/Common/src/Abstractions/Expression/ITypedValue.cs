﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Common.Expression.Internal
{
    public interface ITypedValue
    {
        object Value { get; }

        Type TypeDescriptor { get; }
    }
}
