// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Expression.Internal.Contexts;

public class ServiceExpressionContextAccessor : IPropertyAccessor
{
    public bool CanRead(IEvaluationContext context, object target, string name)
    {
        var serviceType = ServiceFactoryResolver.GetServiceNameAndType(context, name, out var lookupName);
        if (serviceType != null)
        {
            return target is IServiceExpressionContext context1 && context1.ContainsService(lookupName, serviceType);
        }

        return target is IServiceExpressionContext context2 && context2.ContainsService(name);
    }

    public bool CanWrite(IEvaluationContext context, object target, string name)
    {
        return false;
    }

    public IList<Type> GetSpecificTargetClasses()
    {
        return new List<Type> { typeof(IServiceExpressionContext) };
    }

    public ITypedValue Read(IEvaluationContext context, object target, string name)
    {
        if (target is not IServiceExpressionContext targetContext)
        {
            throw new InvalidOperationException("target must be of type IServiceExpressionContext");
        }

        var serviceType = ServiceFactoryResolver.GetServiceNameAndType(context, name, out var lookupName);
        if (serviceType != null)
        {
            return new TypedValue(targetContext.GetService(lookupName, serviceType));
        }

        return new TypedValue(targetContext.GetService(name));
    }

    public void Write(IEvaluationContext context, object target, string name, object newValue)
    {
        throw new AccessException("Services are read-only");
    }
}
