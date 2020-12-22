// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Spring.Ast
{
    public class StringLiteral : Literal
    {
        private readonly ITypedValue _value;

        public StringLiteral(string payload, int startPos, int endPos, string value)
            : base(payload, startPos, endPos)
        {
            var valueWithinQuotes = value.Substring(1, value.Length - 1 - 1);
            valueWithinQuotes = valueWithinQuotes.Replace("''", "'");
            valueWithinQuotes = valueWithinQuotes.Replace("\"\"", "\"");

            // valueWithinQuotes = StringUtils.replace(valueWithinQuotes, "''", "'");
            // valueWithinQuotes = StringUtils.replace(valueWithinQuotes, "\"\"", "\"");
            _value = new TypedValue(valueWithinQuotes);
            _exitTypeDescriptor = "Ljava/lang/String";
        }

        public override ITypedValue GetLiteralValue()
        {
            return _value;
        }

        public override string ToString()
        {
            return "'" + GetLiteralValue().Value + "'";
        }

        public override bool IsCompilable()
        {
            return true;
        }

        public override void GenerateCode(DynamicMethod mv, CodeFlow cf)
        {
            // mv.visitLdcInsn(this.value.getValue());
            //    cf.pushDescriptor(this.exitTypeDescriptor);
        }
    }
}
