// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection.Emit;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public class RealLiteral : Literal
{
    private readonly ITypedValue _value;

    public RealLiteral(string payload, int startPos, int endPos, double value)
        : base(payload, startPos, endPos)
    {
        _value = new TypedValue(value);
        _exitTypeDescriptor = TypeDescriptor.D;
    }

    public override ITypedValue GetLiteralValue()
    {
        return _value;
    }

    public override bool IsCompilable()
    {
        return true;
    }

    public override void GenerateCode(ILGenerator gen, CodeFlow cf)
    {
        gen.Emit(OpCodes.Ldc_R8, (double)_value.Value);
        cf.PushDescriptor(_exitTypeDescriptor);
    }
}