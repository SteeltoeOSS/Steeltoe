﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Common.Expression.Internal;

public class AccessException : Exception
{
    public AccessException(string message)
        : base(message)
    {
    }

    public AccessException(string message, Exception cause)
        : base(message, cause)
    {
    }
}