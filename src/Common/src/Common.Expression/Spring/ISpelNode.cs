// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection.Emit;

namespace Steeltoe.Common.Expression.Internal.Spring
{
    public interface ISpelNode
    {
        int StartPosition { get; }

        int EndPosition { get; }

        int ChildCount { get; }

        bool IsCompilable();

        object GetValue(ExpressionState expressionState);

        ITypedValue GetTypedValue(ExpressionState expressionState);

        bool IsWritable(ExpressionState expressionState);

        void SetValue(ExpressionState expressionState, object newValue);

        string ToStringAST();

        ISpelNode GetChild(int index);

        Type GetObjectType(object obj);

        void GenerateCode(ILGenerator generator, CodeFlow cf);
    }
}
