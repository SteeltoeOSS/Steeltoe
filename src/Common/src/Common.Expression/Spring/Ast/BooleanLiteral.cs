// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Support;
using System.Reflection.Emit;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public class BooleanLiteral : Literal
{
    private readonly BooleanTypedValue _value;

    public BooleanLiteral(string payload, int startPos, int endPos, bool value)
        : base(payload, startPos, endPos)
    {
        _value = BooleanTypedValue.ForValue(value);
        _exitTypeDescriptor = TypeDescriptor.Z;
    }

    public override ITypedValue GetLiteralValue()
    {
        return _value;
    }

    public override bool IsCompilable() => true;

    public override void GenerateCode(ILGenerator gen, CodeFlow cf)
    {
        var result = gen.DeclareLocal(typeof(bool));
        if (_value.Equals(BooleanTypedValue.TRUE))
        {
            gen.Emit(OpCodes.Ldc_I4_1);
        }
        else
        {
            gen.Emit(OpCodes.Ldc_I4_0);
        }

        gen.Emit(OpCodes.Stloc, result);
        gen.Emit(OpCodes.Ldloc, result);
        cf.PushDescriptor(_exitTypeDescriptor);
    }
}