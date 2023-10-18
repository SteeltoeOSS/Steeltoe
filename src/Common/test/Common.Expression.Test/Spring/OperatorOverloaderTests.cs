// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Expression.Internal.Spring;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Xunit;

namespace Steeltoe.Common.Expression.Test.Spring;

public sealed class OperatorOverloaderTests : AbstractExpressionTests
{
    [Fact]
    public void TestSimpleOperations()
    {
        // no built in support for this:
        EvaluateAndCheckError("'abc'-true", SpelMessage.OperatorNotSupportedBetweenTypes);

        StandardEvaluationContext eContext = TestScenarioCreator.GetTestEvaluationContext();
        eContext.OperatorOverloader = new StringAndBooleanAddition();

        var expr = (SpelExpression)Parser.ParseExpression("'abc'+true");
        Assert.Equal("abcTrue", expr.GetValue(eContext));

        expr = (SpelExpression)Parser.ParseExpression("'abc'-true");
        Assert.Equal("abc", expr.GetValue(eContext));

        expr = (SpelExpression)Parser.ParseExpression("'abc'+null");
        Assert.Equal("abcnull", expr.GetValue(eContext));
    }

    public sealed class StringAndBooleanAddition : IOperatorOverloader
    {
        public object Operate(Operation operation, object leftOperand, object rightOperand)
        {
            if (operation == Operation.Add)
            {
                return (string)leftOperand + (bool)rightOperand;
            }

            return leftOperand;
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
