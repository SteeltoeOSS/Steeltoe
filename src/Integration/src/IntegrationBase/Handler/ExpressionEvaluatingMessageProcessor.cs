// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Messaging;
using System;

namespace Steeltoe.Integration.Handler
{
    public class ExpressionEvaluatingMessageProcessor<T> : AbstractMessageProcessor<T>
    {
        private IExpression Expression { get; }

        private Type ExpectedType { get; }

        public ExpressionEvaluatingMessageProcessor(IApplicationContext context, IExpression expression)
        : this(context, expression, typeof(T))
        {
        }

        public ExpressionEvaluatingMessageProcessor(IApplicationContext context, IExpression expression, Type expectedType)
            : base(context)
        {
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
            ExpectedType = expectedType;
        }

        public ExpressionEvaluatingMessageProcessor(IApplicationContext context, string expression)
            : base(context)
        {
            try
            {
                Expression = ExpressionParser.ParseExpression(expression);
                ExpectedType = typeof(T);
            }
            catch (ParseException ex)
            {
                throw new ArgumentException("Failed to parse expression.", ex);
            }
        }

        public ExpressionEvaluatingMessageProcessor(IApplicationContext context, string expression, Type expectedType)
            : base(context)
        {
            try
            {
                Expression = ExpressionParser.ParseExpression(expression);
                ExpectedType = expectedType;
            }
            catch (ParseException ex)
            {
                throw new ArgumentException("Failed to parse expression.", ex);
            }
        }

        public override T ProcessMessage(IMessage message)
        {
            return (T)EvaluateExpression(Expression, message, ExpectedType);
        }

        public override string ToString()
        {
            return $"ExpressionEvaluatingMessageProcessor for: [{Expression.ExpressionString}]";
        }
    }
}
