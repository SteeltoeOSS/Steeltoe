// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
