// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal;

public class EvaluationException : ExpressionException
{
    public EvaluationException(string message)
        : base(message)
    {
    }

    public EvaluationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public EvaluationException(int position, string message)
        : base(position, message)
    {
    }

    public EvaluationException(string expressionString, string message)
        : base(expressionString, message)
    {
    }

    public EvaluationException(int position, string message, Exception innerException)
        : base(position, message, innerException)
    {
    }
}
