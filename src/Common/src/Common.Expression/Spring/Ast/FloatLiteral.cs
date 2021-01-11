// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class FloatLiteral : Literal
    {
        private readonly ITypedValue _value;

        public FloatLiteral(string payload, int startPos, int endPos, float value)
            : base(payload, startPos, endPos)
        {
            _value = new TypedValue(value);
            _exitTypeDescriptor = "F";
        }

        public override ITypedValue GetLiteralValue()
        {
            return _value;
        }

        public override bool IsCompilable() => true;

        public override void GenerateCode(DynamicMethod mv, CodeFlow cf)
        {
            // mv.visitLdcInsn(this._value.getValue());
            // cf.pushDescriptor(this.exitTypeDescriptor);
        }
    }
}
