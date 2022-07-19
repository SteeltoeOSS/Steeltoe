// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection.Emit;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class NullLiteral : Literal
{
    public NullLiteral(int startPos, int endPos)
        : base(null, startPos, endPos)
    {
        exitTypeDescriptor = TypeDescriptor.Object;
    }

    public override ITypedValue GetLiteralValue() => TypedValue.Null;

    public override string ToString() => "null";

    public override bool IsCompilable() => true;

    public override void GenerateCode(ILGenerator gen, CodeFlow cf)
    {
        gen.Emit(OpCodes.Ldnull);
        cf.PushDescriptor(exitTypeDescriptor);
    }
}
