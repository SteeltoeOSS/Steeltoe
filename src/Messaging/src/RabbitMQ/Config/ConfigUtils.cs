// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;

namespace Steeltoe.Messaging.RabbitMQ.Config;

internal static class ConfigUtils
{
    private static readonly Regex Regex = new(@"\s+");

    public static bool IsExpression(string expression)
    {
        if (expression.StartsWith("#{") && expression.EndsWith("}"))
        {
            return true;
        }

        return false;
    }

    public static string ExtractExpressionString(string expression)
    {
        expression = expression.Substring(2, expression.Length - 2 - 1);
        return ReplaceWhitespace(expression, string.Empty);
    }

    public static bool IsServiceReference(string expression)
    {
        return expression[0] == '@';
    }

    public static string ExtractServiceName(string expression)
    {
        if (expression[0] == '@')
        {
            return expression.Substring(1);
        }

        return expression;
    }

    public static string ReplaceWhitespace(string input, string replacement)
    {
        return Regex.Replace(input, replacement);
    }
}
