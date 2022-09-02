// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Steeltoe.Common;

public static class ArgumentGuard
{
    public static void NotNull<T>([ValidatedNotNull] [NotNull] T? value, [CallerArgumentExpression("value")] string? parameterName = null)
    {
        AssertNoDotInParameterName(parameterName);

        ArgumentNullException.ThrowIfNull(value, parameterName);
    }

    public static void NotNullOrEmpty<T>([ValidatedNotNull] [NotNull] IEnumerable<T>? value, [CallerArgumentExpression("value")] string? parameterName = null)
    {
        AssertNoDotInParameterName(parameterName);

        ArgumentNullException.ThrowIfNull(value, parameterName);

        if (!value.Any())
        {
            throw new ArgumentException("Collection cannot be empty.", parameterName);
        }
    }

    public static void NotNullOrEmpty([ValidatedNotNull] [NotNull] string? value, [CallerArgumentExpression("value")] string? parameterName = null)
    {
        AssertNoDotInParameterName(parameterName);

        ArgumentNullException.ThrowIfNull(value, parameterName);

        if (value == string.Empty)
        {
            throw new ArgumentException("String cannot be empty.", parameterName);
        }
    }

    public static void NotNullOrWhiteSpace([ValidatedNotNull] [NotNull] string? value, [CallerArgumentExpression("value")] string? parameterName = null)
    {
        AssertNoDotInParameterName(parameterName);

        ArgumentNullException.ThrowIfNull(value, parameterName);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("String cannot be empty or contain only whitespace.", parameterName);
        }
    }

    public static void ElementsNotNull<T>(IEnumerable<T?>? elements, [CallerArgumentExpression("elements")] string? parameterName = null)
        where T : class
    {
        AssertNoDotInParameterName(parameterName);

        if (elements != null && elements.Any(element => element == null))
        {
            throw new ArgumentException("Collection cannot contain nulls.", parameterName);
        }
    }

    public static void ElementsNotNullOrEmpty(IEnumerable<string?>? elements, [CallerArgumentExpression("elements")] string? parameterName = null)
    {
        AssertNoDotInParameterName(parameterName);

        if (elements != null && elements.Any(string.IsNullOrEmpty))
        {
            throw new ArgumentException("Collection cannot contain nulls or empty strings.", parameterName);
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
