// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal;

namespace Steeltoe.Integration.Expression;

public interface IEvaluationCallback
{
    object Evaluate(IExpression expression);
}