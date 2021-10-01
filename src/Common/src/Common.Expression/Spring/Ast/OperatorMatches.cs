// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Support;
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class OperatorMatches : Operator
    {
        private readonly ConcurrentDictionary<string, Regex> _patternCache = new ();

        public OperatorMatches(int startPos, int endPos, params SpelNode[] operands)
        : base("matches", startPos, endPos, operands)
        {
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            var leftOp = LeftOperand;
            var rightOp = RightOperand;
            var left = leftOp.GetValue<string>(state);
            var right = RightOperand.GetValue(state);

            if (left == null)
            {
                throw new SpelEvaluationException(leftOp.StartPosition, SpelMessage.INVALID_FIRST_OPERAND_FOR_MATCHES_OPERATOR, (object)null);
            }

            if (!(right is string))
            {
                throw new SpelEvaluationException(rightOp.StartPosition, SpelMessage.INVALID_SECOND_OPERAND_FOR_MATCHES_OPERATOR, right);
            }

            try
            {
                var rightString = (string)right;
                _patternCache.TryGetValue(rightString, out var pattern);
                if (pattern == null)
                {
                    pattern = new Regex(rightString, RegexOptions.Compiled, TimeSpan.FromSeconds(1));
                    _patternCache.TryAdd(rightString, pattern);
                }

                return BooleanTypedValue.ForValue(pattern.IsMatch(left));
            }
            catch (ArgumentException ex)
            {
                throw new SpelEvaluationException(rightOp.StartPosition, ex, SpelMessage.INVALID_PATTERN, right);
            }
            catch (RegexMatchTimeoutException ex)
            {
                throw new SpelEvaluationException(rightOp.StartPosition, ex, SpelMessage.FLAWED_PATTERN, right);
            }
        }
    }
}
