// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Standard;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring;

public static class SpelUtilities
{
    public static void PrintAbstractSyntaxTree(TextWriter printStream, IExpression expression)
    {
        printStream.WriteLine($"===> Expression '{expression.ExpressionString}' - AST start");
        PrintAst(printStream, ((SpelExpression)expression).Ast, string.Empty);
        printStream.WriteLine($"===> Expression '{expression.ExpressionString}' - AST end");
    }

    private static void PrintAst(TextWriter output, ISpelNode t, string indent)
    {
        if (t != null)
        {
            var sb = new StringBuilder();
            sb.Append(indent).Append(t.GetType().Name);
            sb.Append("  value:").Append(t.ToStringAst());
            sb.Append(t.ChildCount < 2 ? string.Empty : $"  #children:{t.ChildCount}");
            output.WriteLine(sb.ToString());
            for (var i = 0; i < t.ChildCount; i++)
            {
                PrintAst(output, t.GetChild(i), $"{indent}  ");
            }
        }
    }
}
