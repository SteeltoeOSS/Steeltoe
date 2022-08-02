// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection.Emit;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class IntLiteral : Literal
{
    private readonly ITypedValue _value;

    public IntLiteral(string payload, int startPos, int endPos, int value)
        : base(payload, startPos, endPos)
    {
        _value = new TypedValue(value);
        exitTypeDescriptor = TypeDescriptor.I;
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
        object intVal = _value.Value;

        if (intVal == null)
        {
            throw new InvalidOperationException("No int value");
        }

        int intValue = (int)intVal;

        if (intValue == -1)
        {
            // Not sure we can get here because -1 is OpMinus
            gen.Emit(OpCodes.Ldc_I4_M1);
        }
        else
        {
            gen.Emit(OpCodes.Ldc_I4, intValue);
        }

        cf.PushDescriptor(exitTypeDescriptor);
    }
}
