// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Common.Expression
{
    public class ExpressionException : Exception
    {
        protected string ExpressionString { get; set; }

        protected int Position { get; set; }

        public ExpressionException(string message)
        : base(message)
        {
            ExpressionString = null;
            Position = 0;
        }

        public ExpressionException(string message, Exception cause)
        : base(message, cause)
        {
            ExpressionString = null;
            Position = 0;
        }

        public ExpressionException(string expressionString, string message)
        : base(message)
        {
            ExpressionString = expressionString;
            Position = -1;
        }

        public ExpressionException(string expressionString, int position, string message)
        : base(message)
        {
            ExpressionString = expressionString;
            Position = position;
        }

        public ExpressionException(int position, string message)
        : base(message)
        {
            ExpressionString = null;
            Position = position;
        }

        public ExpressionException(int position, string message, Exception cause)
            : base(message, cause)
        {
            ExpressionString = null;
            Position = position;
        }
    }
}
