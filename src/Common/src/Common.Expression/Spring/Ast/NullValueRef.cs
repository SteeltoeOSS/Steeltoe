﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public class NullValueRef : IValueRef
{
    public static readonly NullValueRef INSTANCE = new ();

    public ITypedValue GetValue() => TypedValue.NULL;

    public void SetValue(object newValue)
    {
        // The exception position '0' isn't right but the overhead of creating
        // instances of this per node (where the node is solely for error reporting)
        // would be unfortunate.
        throw new SpelEvaluationException(0, SpelMessage.NOT_ASSIGNABLE, "null");
    }

    public bool IsWritable => false;
}