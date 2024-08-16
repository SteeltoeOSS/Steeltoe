// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Steeltoe.Common;

internal static class ArgumentGuard
{
    public static void ElementsNotNull<T>(IEnumerable<T?>? elements, [CallerArgumentExpression("elements")] string? parameterName = null)
        where T : class
    {
        AssertNoDotInParameterName(parameterName);

        if (elements != null && elements.Any(element => element == null))
        {
            throw new ArgumentException("Collection element cannot be null.", parameterName);
        }
    }

    public static void ElementsNotNullOrEmpty(IEnumerable<string?>? elements, [CallerArgumentExpression("elements")] string? parameterName = null)
    {
        AssertNoDotInParameterName(parameterName);

        if (elements != null && elements.Any(string.IsNullOrEmpty))
        {
            throw new ArgumentException("Collection element cannot be null or an empty string.", parameterName);
        }
    }

    public static void ElementsNotNullOrWhiteSpace(IEnumerable<string?>? elements, [CallerArgumentExpression("elements")] string? parameterName = null)
    {
        AssertNoDotInParameterName(parameterName);

        if (elements != null && elements.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Collection element cannot be null, an empty string, or composed entirely of whitespace.", parameterName);
        }
    }

    [Conditional("DEBUG")]
    private static void AssertNoDotInParameterName(string? parameterName)
    {
        if (parameterName != null && parameterName.Contains('.'))
        {
            throw new InvalidOperationException("Only use this method to verify the parameter itself, not its members.");
        }
    }
}
