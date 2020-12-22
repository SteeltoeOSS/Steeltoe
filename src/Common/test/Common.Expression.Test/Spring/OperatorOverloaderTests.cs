// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Spring.Standard;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Expression.Spring
{
    public class OperatorOverloaderTests : AbstractExpressionTests
    {
        [Fact]
        public void TestSimpleOperations()
        {
            // no built in support for this:
            EvaluateAndCheckError("'abc'-true", SpelMessage.OPERATOR_NOT_SUPPORTED_BETWEEN_TYPES);

            var eContext = TestScenarioCreator.GetTestEvaluationContext();
            eContext.OperatorOverloader = new StringAndBooleanAddition();

            var expr = (SpelExpression)parser.ParseExpression("'abc'+true");
            Assert.Equal("abcTrue", expr.GetValue(eContext));

            expr = (SpelExpression)parser.ParseExpression("'abc'-true");
            Assert.Equal("abc", expr.GetValue(eContext));

            expr = (SpelExpression)parser.ParseExpression("'abc'+null");
            Assert.Equal("abcnull", expr.GetValue(eContext));
        }

        public class StringAndBooleanAddition : IOperatorOverloader
        {
            public object Operate(Operation operation, object leftOperand, object rightOperand)
            {
                if (operation == Operation.ADD)
                {
                    return ((string)leftOperand) + ((bool)rightOperand).ToString();
                }
                else
                {
                    return leftOperand;
                }
            }

            public bool OverridesOperation(Operation operation, object leftOperand, object rightOperand)
            {
                if (leftOperand is string && rightOperand is bool)
                {
                    return true;
                }

                return false;
            }
        }
    }
}
