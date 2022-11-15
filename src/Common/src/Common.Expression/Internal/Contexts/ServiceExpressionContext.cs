// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;

namespace Steeltoe.Common.Expression.Internal.Contexts;

// Used as root context with StandardServiceExpressionResolver
public class ServiceExpressionContext : IServiceExpressionContext
{
    private const string EnvironmentName = "configuration";

    public IApplicationContext ApplicationContext { get; }

    public ServiceExpressionContext(IApplicationContext applicationContext)
    {
        ArgumentGuard.NotNull(applicationContext);

        ApplicationContext = applicationContext;
    }

    public bool ContainsService(string serviceName, Type serviceType)
    {
        if (serviceName == EnvironmentName)
        {
            return true;
        }

        return ApplicationContext.ContainsService(serviceName, serviceType);
    }

    public bool ContainsService(string serviceName)
    {
        if (serviceName == EnvironmentName)
        {
            return true;
        }

        return ApplicationContext.ContainsService(serviceName);
    }

    public object GetService(string serviceName)
    {
        if (serviceName == EnvironmentName)
        {
            return ApplicationContext.Configuration;
        }

        if (ApplicationContext.ContainsService(serviceName))
        {
            return ApplicationContext.GetService(serviceName);
        }

        return null;
    }

    public object GetService(string serviceName, Type serviceType)
    {
        if (serviceName == EnvironmentName)
        {
            return ApplicationContext.Configuration;
        }

        if (ApplicationContext.ContainsService(serviceName, serviceType))
        {
            return ApplicationContext.GetService(serviceName, serviceType);
        }

        return null;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not ServiceExpressionContext otherContext)
        {
            return false;
        }

        return ApplicationContext == otherContext.ApplicationContext;
    }

    public override int GetHashCode()
    {
        return ApplicationContext.GetHashCode();
    }
}
