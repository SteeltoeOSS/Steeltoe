// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Expression.Internal.Contexts
{
    public class ServiceFactoryAccessor : IPropertyAccessor
    {
        public bool CanRead(IEvaluationContext context, object target, string name)
        {
            var serviceType = ServiceFactoryResolver.GetServiceNameAndType(context, name, out var lookupName);
            if (serviceType != null)
            {
                return target is IApplicationContext && ((IApplicationContext)target).ContainsService(lookupName, serviceType);
            }

            return target is IApplicationContext && ((IApplicationContext)target).ContainsService(name);
        }

        public bool CanWrite(IEvaluationContext context, object target, string name)
        {
            return false;
        }

        public IList<Type> GetSpecificTargetClasses()
        {
            return new List<Type>() { typeof(IApplicationContext) };
        }

        public ITypedValue Read(IEvaluationContext context, object target, string name)
        {
            var targetContext = target as IApplicationContext;
            if (targetContext == null)
            {
                throw new InvalidOperationException("target must be of type IApplicationContext");
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
            throw new AccessException("Services in a IApplicationContext are read-only");
        }
    }
}
