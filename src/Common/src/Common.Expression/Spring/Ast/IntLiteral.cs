// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Spring.Ast
{
    public class IntLiteral : Literal
    {
        private readonly ITypedValue _value;

        public IntLiteral(string payload, int startPos, int endPos, int value)
            : base(payload, startPos, endPos)
        {
            _value = new TypedValue(value);
            _exitTypeDescriptor = "I";
        }

        public override ITypedValue GetLiteralValue() => _value;

        public override bool IsCompilable() => true;

        public override void GenerateCode(DynamicMethod mv, CodeFlow cf)
        {
            // Integer intValue = (Integer)this.value.getValue();
            // Assert.state(intValue != null, "No int value");
            // if (intValue == -1)
            // {
            //    // Not sure we can get here because -1 is OpMinus
            //    mv.visitInsn(ICONST_M1);
            // }
            // else if (intValue >= 0 && intValue < 6)
            // {
            //    mv.visitInsn(ICONST_0 + intValue);
            // }
            // else
            // {
            //    mv.visitLdcInsn(intValue);
            // }
            // cf.pushDescriptor(this.exitTypeDescriptor);
        }
    }
}
