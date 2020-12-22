// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Common.Expression
{
    public class ExpressionInvocationTargetException : EvaluationException
    {
        public ExpressionInvocationTargetException(int position, string message, Exception cause)
            : base(position, message, cause)
        {
        }

        public ExpressionInvocationTargetException(int position, string message)
            : base(position, message)
        {
        }

        public ExpressionInvocationTargetException(string expressionString, string message)
            : base(expressionString, message)
        {
        }

        public ExpressionInvocationTargetException(string message, Exception cause)
            : base(message, cause)
        {
        }

        public ExpressionInvocationTargetException(string message)
            : base(message)
        {
        }
    }
}
