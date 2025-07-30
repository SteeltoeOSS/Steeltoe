// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal.Spring.Support;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public class StandardOperatorOverloader : IOperatorOverloader
{
    public bool OverridesOperation(Operation operation, object leftOperand, object rightOperand)
    {
        return false;
    }

    public object Operate(Operation operation, object leftOperand, object rightOperand)
    {
        throw new EvaluationException("No operation overloaded by default");
    }
}