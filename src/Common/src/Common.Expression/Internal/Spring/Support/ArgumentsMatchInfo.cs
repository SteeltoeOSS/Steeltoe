// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal.Spring.Support;

public class ArgumentsMatchInfo
{
    private readonly ArgumentsMatchKind _kind;

    public bool IsExactMatch => _kind == ArgumentsMatchKind.Exact;

    public bool IsCloseMatch => _kind == ArgumentsMatchKind.Close;

    public bool IsMatchRequiringConversion => _kind == ArgumentsMatchKind.RequiresConversion;

    public ArgumentsMatchInfo(ArgumentsMatchKind kind)
    {
        _kind = kind;
    }

    public override string ToString()
    {
        return $"ArgumentMatchInfo: {_kind}";
    }
}
