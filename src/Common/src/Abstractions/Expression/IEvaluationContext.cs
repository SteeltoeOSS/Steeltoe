// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression
{
    /// <summary>
    /// Expressions are executed in an evaluation context. It is in this context that
    /// references are resolved when encountered during expression evaluation.
    /// TODO:  This interface is not complete
    /// </summary>
    public interface IEvaluationContext
    {
        ITypeConverter TypeConverter { get; }
    }
}
