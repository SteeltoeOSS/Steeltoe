// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class Assign : SpelNode
    {
        public Assign(int startPos, int endPos, params SpelNode[] operands)
        : base(startPos, endPos, operands)
        {
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            var newValue = _children[1].GetValueInternal(state);
            GetChild(0).SetValue(state, newValue.Value);
            return newValue;
        }

        public override string ToStringAST()
        {
            return GetChild(0).ToStringAST() + "=" + GetChild(1).ToStringAST();
        }
    }
}
