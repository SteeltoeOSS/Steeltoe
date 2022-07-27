// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using System;

namespace Steeltoe.Common.Expression.Internal.Contexts;

// Used as root context with StandardServiceExpressionResolver
public class ServiceExpressionContext : IServiceExpressionContext
{
    private const string _environmentName = "configuration";

    private readonly IApplicationContext _applicationContext;

    public ServiceExpressionContext(IApplicationContext applicationContext)
    {
        if (applicationContext == null)
        {
            throw new ArgumentNullException(nameof(applicationContext));
        }

        _applicationContext = applicationContext;
    }

    public IApplicationContext ApplicationContext => _applicationContext;

    public bool ContainsService(string serviceName, Type serviceType)
    {
        if (serviceName == _environmentName)
        {
            return true;
        }

        return _applicationContext.ContainsService(serviceName, serviceType);
    }

    public bool ContainsService(string serviceName)
    {
        if (serviceName == _environmentName)
        {
            return true;
        }

        return _applicationContext.ContainsService(serviceName);
    }

    public object GetService(string serviceName)
    {
        if (serviceName == _environmentName)
        {
            return _applicationContext.Configuration;
        }

        if (_applicationContext.ContainsService(serviceName))
        {
            return _applicationContext.GetService(serviceName);
        }

        return null;
    }

    public object GetService(string serviceName, Type serviceType)
    {
        if (serviceName == _environmentName)
        {
            return _applicationContext.Configuration;
        }

        if (_applicationContext.ContainsService(serviceName, serviceType))
        {
            return _applicationContext.GetService(serviceName, serviceType);
        }

        return null;
    }

    public override bool Equals(object other)
    {
        if (this == other)
        {
            return true;
        }

        if (other is not ServiceExpressionContext)
        {
            return false;
        }

        var otherContext = (ServiceExpressionContext)other;
        return _applicationContext == otherContext._applicationContext;
    }

    public override int GetHashCode()
    {
        return _applicationContext.GetHashCode();
    }
}