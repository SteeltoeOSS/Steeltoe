// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Spring.Ast
{
    public class NullLiteral : Literal
    {
        public NullLiteral(int startPos, int endPos)
        : base(null, startPos, endPos)
        {
            _exitTypeDescriptor = "LSystem/Object";
        }

        public override ITypedValue GetLiteralValue() => TypedValue.NULL;

        public override string ToString() => "null";

        public override bool IsCompilable() => true;

        public override void GenerateCode(DynamicMethod mv, CodeFlow cf)
        {
            // mv.visitInsn(ACONST_NULL);
            //    cf.pushDescriptor(this.exitTypeDescriptor);
        }
    }
}
