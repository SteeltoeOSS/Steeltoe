// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using System;

namespace Steeltoe.Integration.Util
{
    public abstract class AbstractExpressionEvaluator
    {
        public IExpressionParser ExpressionParser { get; set; }

        public IEvaluationContext EvaluationContext { get; set; }

        public IMessageBuilderFactory MessageBuilderFactory { get; set; } = new DefaultMessageBuilderFactory();

        public ITypeConverter TypeConverter { get; set; }

        protected AbstractExpressionEvaluator(IExpressionParser expressionParser, IEvaluationContext evaluationContext)
        {
            ExpressionParser = expressionParser ?? throw new ArgumentNullException(nameof(expressionParser));
            EvaluationContext = evaluationContext ?? throw new ArgumentNullException(nameof(evaluationContext));
        }

        protected T EvaluateExpression<T>(IExpression expression, IMessage message)
        {
            return (T)EvaluateExpression(expression, message, typeof(T));
        }

        protected object EvaluateExpression(IExpression expression, IMessage message, Type expectedType)
        {
            try
            {
                return EvaluateExpression(expression, (object)message, expectedType);
            }
            catch (Exception ex)
            {
                Exception cause = null;
                if (ex is EvaluationException)
                { // NOSONAR
                    cause = ex.InnerException;
                }

                var wrapped = IntegrationUtils.WrapInHandlingExceptionIfNecessary(message, "Expression evaluation failed: " + expression.ExpressionString, cause ?? ex);
                if (wrapped != ex)
                {
                    throw wrapped;
                }

                throw;
            }
        }

        protected object EvaluateExpression(string expression, object input)
        {
            return EvaluateExpression(expression, input, null);
        }

        protected object EvaluateExpression(string expression, object input, Type expectedType)
        {
            return ExpressionParser.ParseExpression(expression).GetValue(EvaluationContext, input, expectedType);
        }

        protected object EvaluateExpression(IExpression expression)
        {
            return expression.GetValue(EvaluationContext);
        }

        protected object EvaluateExpression(IExpression expression, object input, Type expectedType)
        {
            return expression.GetValue(EvaluationContext, input, expectedType);
        }

        protected T EvaluateExpression<T>(IExpression expression, object input)
        {
            return (T)EvaluateExpression(expression, input, typeof(T));
        }
    }
}
