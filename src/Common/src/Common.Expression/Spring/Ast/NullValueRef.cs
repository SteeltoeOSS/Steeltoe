// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class NullValueRef : IValueRef
{
    public static readonly NullValueRef Instance = new ();

    public ITypedValue GetValue() => TypedValue.Null;

    public void SetValue(object newValue)
    {
        // The exception position '0' isn't right but the overhead of creating
        // instances of this per node (where the node is solely for error reporting)
        // would be unfortunate.
        throw new SpelEvaluationException(0, SpelMessage.NotAssignable, "null");
    }

    public bool IsWritable => false;
}
