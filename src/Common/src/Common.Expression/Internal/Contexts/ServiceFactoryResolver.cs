// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;

namespace Steeltoe.Common.Expression.Internal.Contexts;

public class ServiceFactoryResolver : IServiceResolver
{
    private readonly IApplicationContext _applicationContext;

    public ServiceFactoryResolver(IApplicationContext applicationContext)
    {
        ArgumentGuard.NotNull(applicationContext);

        _applicationContext = applicationContext;
    }

    public static (Type Type, string Name) GetServiceNameAndType(IEvaluationContext context, string serviceName)
    {
        Type result = null;
        string name = serviceName;

        if (serviceName.StartsWith("T(", StringComparison.Ordinal) && context.TypeLocator != null)
        {
            int endParen = serviceName.LastIndexOf(')');

            if (endParen > 0)
            {
                string serviceTypeName = serviceName.Substring(2, endParen - 2);
                result = context.TypeLocator.FindType(serviceTypeName);
                name = serviceName.Substring(endParen + 1);
            }
        }

        return (result, name);
    }

    public object Resolve(IEvaluationContext context, string serviceName)
    {
        try
        {
            (Type serviceType, string lookupName) = GetServiceNameAndType(context, serviceName);
            object result = serviceType != null ? _applicationContext.GetService(lookupName, serviceType) : _applicationContext.GetService(serviceName);

            if (result == null)
            {
                throw new AccessException($"Could not resolve service reference '{serviceName}' against application context");
            }

            return result;
        }
        catch (Exception ex)
        {
            throw new AccessException("Could not resolve service reference against application context", ex);
        }
    }
}
