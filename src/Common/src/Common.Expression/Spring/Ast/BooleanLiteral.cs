// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Spring.Support;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Spring.Ast
{
    public class BooleanLiteral : Literal
    {
        private readonly BooleanTypedValue _value;

        public BooleanLiteral(string payload, int startPos, int endPos, bool value)
            : base(payload, startPos, endPos)
        {
            _value = BooleanTypedValue.ForValue(value);
            _exitTypeDescriptor = "Z";
        }

        public override ITypedValue GetLiteralValue()
        {
            return _value;
        }

        public override bool IsCompilable() => true;

        public override void GenerateCode(DynamicMethod mv, CodeFlow cf)
        {
            // if (this.value == BooleanTypedValue.TRUE)
            // {
            //    mv.visitLdcInsn(1);
            // }
            // else
            // {
            //    mv.visitLdcInsn(0);
            // }
            // cf.pushDescriptor(this.exitTypeDescriptor);
        }
    }
}
