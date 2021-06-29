// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal
{
    /// <summary>
    /// Parses expression strings into compiled expressions that can be evaluated.
    /// Supports parsing templates as well as standard expression strings.
    /// TODO:  This interface is not complete
    /// </summary>
    public interface IExpressionParser
    {
        IExpression ParseExpression(string expressionString);

        IExpression ParseExpression(string expressionString, IParserContext context);
    }
}
