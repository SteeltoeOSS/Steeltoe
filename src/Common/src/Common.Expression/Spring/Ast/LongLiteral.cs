// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Spring.Ast
{
    public class LongLiteral : Literal
    {
        private readonly ITypedValue _value;

        public LongLiteral(string payload, int startPos, int endPos, long value)
            : base(payload, startPos, endPos)
        {
            _value = new TypedValue(value);
            _exitTypeDescriptor = "J";
        }

        public override ITypedValue GetLiteralValue() => _value;

        public override bool IsCompilable() => true;

        public override void GenerateCode(DynamicMethod mv, CodeFlow cf)
        {
            // mv.visitLdcInsn(this.value.getValue());
            //    cf.pushDescriptor(this.exitTypeDescriptor);
        }
    }
}
