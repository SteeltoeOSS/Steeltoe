// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection.Emit;
using Steeltoe.Common.Expression.Internal.Spring.Support;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class BooleanLiteral : Literal
{
    private readonly BooleanTypedValue _value;

    public BooleanLiteral(string payload, int startPos, int endPos, bool value)
        : base(payload, startPos, endPos)
    {
        _value = BooleanTypedValue.ForValue(value);
        exitTypeDescriptor = TypeDescriptor.Z;
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
        LocalBuilder result = gen.DeclareLocal(typeof(bool));
        gen.Emit(_value.Equals(BooleanTypedValue.True) ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);

        gen.Emit(OpCodes.Stloc, result);
        gen.Emit(OpCodes.Ldloc, result);
        cf.PushDescriptor(exitTypeDescriptor);
    }
}
