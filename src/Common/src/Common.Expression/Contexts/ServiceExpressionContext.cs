// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using System;

namespace Steeltoe.Common.Expression.Internal.Contexts
{
    // Used as root context with StandardServiceExpressionResolver
    public class ServiceExpressionContext : IServiceExpressionContext
    {
        private const string _environmentName = "configuration";

        public ServiceExpressionContext(IApplicationContext applicationContext)
        {
            ApplicationContext = applicationContext ?? throw new ArgumentNullException(nameof(applicationContext));
        }

        public IApplicationContext ApplicationContext { get; }

        public bool ContainsService(string serviceName, Type serviceType)
        {
            if (serviceName == _environmentName)
            {
                return true;
            }

            return ApplicationContext.ContainsService(serviceName, serviceType);
        }

        public bool ContainsService(string serviceName)
        {
            if (serviceName == _environmentName)
            {
                return true;
            }

            return ApplicationContext.ContainsService(serviceName);
        }

        public object GetService(string serviceName)
        {
            if (serviceName == _environmentName)
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
            if (serviceName == _environmentName)
            {
                return ApplicationContext.Configuration;
            }

            if (ApplicationContext.ContainsService(serviceName, serviceType))
            {
                return ApplicationContext.GetService(serviceName, serviceType);
            }

            return null;
        }

        public override bool Equals(object other)
        {
            if (this == other)
            {
                return true;
            }

            if (other is not ServiceExpressionContext otherContext)
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
}
