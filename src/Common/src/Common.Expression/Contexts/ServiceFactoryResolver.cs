// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using System;

namespace Steeltoe.Common.Expression.Internal.Contexts
{
    public class ServiceFactoryResolver : IServiceResolver
    {
        private readonly IApplicationContext _applicationContext;

        public ServiceFactoryResolver(IApplicationContext applicationContext)
        {
            if (applicationContext == null)
            {
                throw new ArgumentNullException(nameof(applicationContext));
            }

            _applicationContext = applicationContext;
        }

        public static Type GetServiceNameAndType(IEvaluationContext context, string serviceName, out string name)
        {
            Type result = null;
            name = serviceName;
            if (serviceName.StartsWith("T(") && context.TypeLocator != null)
            {
                var endParen = serviceName.LastIndexOf(")");
                if (endParen > 0)
                {
                    var serviceTypeName = serviceName.Substring(2, endParen - 2);
                    result = context.TypeLocator.FindType(serviceTypeName);
                    name = serviceName.Substring(endParen + 1);
                }
            }

            return result;
        }

        public object Resolve(IEvaluationContext context, string serviceName)
        {
            try
            {
                object result = null;
                var serviceType = GetServiceNameAndType(context, serviceName, out var lookupName);
                if (serviceType != null)
                {
                    result = _applicationContext.GetService(lookupName, serviceType);
                }
                else
                {
                    result = _applicationContext.GetService(serviceName);
                }

                if (result == null)
                {
                    throw new AccessException("Could not resolve service reference '" + serviceName + "' against application context");
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new AccessException("Could not resolve service reference against application context", ex);
            }
        }
    }
}
