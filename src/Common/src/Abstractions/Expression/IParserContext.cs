// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal;

/// <summary>
/// Context that gets passed along a bean definition parsing process,
/// encapsulating all relevant configuration as well as state.
/// TODO:  This interface is not complete.
/// </summary>
public interface IParserContext
{
    bool IsTemplate { get; }

    string ExpressionPrefix { get; }

    string ExpressionSuffix { get; }
}
