// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal;

public class ExpressionInvocationTargetException : EvaluationException
{
    public ExpressionInvocationTargetException(string message)
        : base(message)
    {
    }

    public ExpressionInvocationTargetException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ExpressionInvocationTargetException(int position, string message)
        : base(position, message)
    {
    }

    public ExpressionInvocationTargetException(int position, string message, Exception innerException)
        : base(position, message, innerException)
    {
    }

    public ExpressionInvocationTargetException(string expressionString, string message)
        : base(expressionString, message)
    {
    }
}
