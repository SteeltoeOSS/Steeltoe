// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Steeltoe.Common.Expression.Internal.Spring.Support;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class OperatorMatches : Operator
{
    private readonly ConcurrentDictionary<string, Regex> _patternCache = new();

    public OperatorMatches(int startPos, int endPos, params SpelNode[] operands)
        : base("matches", startPos, endPos, operands)
    {
    }

    public override ITypedValue GetValueInternal(ExpressionState state)
    {
        SpelNode leftOp = LeftOperand;
        SpelNode rightOp = RightOperand;
        string left = leftOp.GetValue<string>(state);
        object right = RightOperand.GetValue(state);

        if (left == null)
        {
            throw new SpelEvaluationException(leftOp.StartPosition, SpelMessage.InvalidFirstOperandForMatchesOperator, (object)null);
        }

        if (right is not string rightString)
        {
            throw new SpelEvaluationException(rightOp.StartPosition, SpelMessage.InvalidSecondOperandForMatchesOperator, right);
        }

        try
        {
            _patternCache.TryGetValue(rightString, out Regex pattern);

            if (pattern == null)
            {
                pattern = new Regex(rightString, RegexOptions.Compiled, TimeSpan.FromSeconds(1));
                _patternCache.TryAdd(rightString, pattern);
            }

            return BooleanTypedValue.ForValue(pattern.IsMatch(left));
        }
        catch (ArgumentException ex)
        {
            throw new SpelEvaluationException(rightOp.StartPosition, ex, SpelMessage.InvalidPattern, rightString);
        }
        catch (RegexMatchTimeoutException ex)
        {
            throw new SpelEvaluationException(rightOp.StartPosition, ex, SpelMessage.FlawedPattern, rightString);
        }
    }
}
