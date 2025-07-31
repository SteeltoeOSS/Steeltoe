// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection.Emit;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public class NullLiteral : Literal
{
    public NullLiteral(int startPos, int endPos)
        : base(null, startPos, endPos)
    {
        _exitTypeDescriptor = TypeDescriptor.OBJECT;
    }

    public override ITypedValue GetLiteralValue() => TypedValue.NULL;

    public override string ToString() => "null";

    public override bool IsCompilable() => true;

    public override void GenerateCode(ILGenerator gen, CodeFlow cf)
    {
        gen.Emit(OpCodes.Ldnull);
        cf.PushDescriptor(_exitTypeDescriptor);
    }
}