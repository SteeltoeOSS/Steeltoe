// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal.Spring.Support;

public enum ArgumentsMatchKind
{
    EXACT,
    CLOSE,
    REQUIRES_CONVERSION
}

public class ArgumentsMatchInfo
{
    private readonly ArgumentsMatchKind _kind;

    public ArgumentsMatchInfo(ArgumentsMatchKind kind)
    {
        _kind = kind;
    }

    public bool IsExactMatch => _kind == ArgumentsMatchKind.EXACT;

    public bool IsCloseMatch => _kind == ArgumentsMatchKind.CLOSE;

    public bool IsMatchRequiringConversion => _kind == ArgumentsMatchKind.REQUIRES_CONVERSION;

    public override string ToString()
    {
        return $"ArgumentMatchInfo: {_kind}";
    }
}
