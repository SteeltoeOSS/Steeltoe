// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Reflection;

namespace Steeltoe.Connector;

internal static class ServiceInfoCreatorFactory
{
    private static readonly object Lock = new ();
    private static ServiceInfoCreator _serviceInfoCreator;

    /// <summary>
    /// Build or return the relevant <see cref="ServiceInfoCreator"/>.
    /// </summary>
    /// <param name="configuration">Application <see cref="IConfiguration"/>.</param>
    /// <returns>Singleton <see cref="ServiceInfoCreator"/>.</returns>
    internal static ServiceInfoCreator GetServiceInfoCreator(IConfiguration configuration)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        lock (Lock)
        {
            if (_serviceInfoCreator != null && configuration == _serviceInfoCreator.Configuration && (bool)_serviceInfoCreator.GetType().GetProperty("IsRelevant").GetValue(null))
            {
                return _serviceInfoCreator;
            }

            var alternateInfoCreators = ReflectionHelpers.FindTypeFromAssemblyAttribute<ServiceInfoCreatorAssemblyAttribute>();
            foreach (var alternateInfoCreator in alternateInfoCreators)
            {
                if ((bool)alternateInfoCreator.GetProperty("IsRelevant").GetValue(null))
                {
                    _serviceInfoCreator = (ServiceInfoCreator)alternateInfoCreator.GetMethod(nameof(ServiceInfoCreator.Instance)).Invoke(null, new[] { configuration });
                    return _serviceInfoCreator;
                }
            }

            _serviceInfoCreator = ServiceInfoCreator.Instance(configuration);
        }

        return _serviceInfoCreator;
    }
}
