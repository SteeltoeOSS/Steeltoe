// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal;

public interface IPropertyAccessor
{
    IList<Type> GetSpecificTargetClasses();

    bool CanRead(IEvaluationContext context, object target, string name);

    ITypedValue Read(IEvaluationContext context, object target, string name);

    bool CanWrite(IEvaluationContext context, object target, string name);

    void Write(IEvaluationContext context, object target, string name, object newValue);
}
