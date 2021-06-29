// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class Identifier : SpelNode
    {
        private readonly ITypedValue _id;

        public Identifier(string payload, int startPos, int endPos)
            : base(startPos, endPos)
        {
            _id = new TypedValue(payload);
        }

        public override string ToStringAST()
        {
            return _id.Value != null ? _id.Value.ToString() : "null";
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            return _id;
        }
    }
}
