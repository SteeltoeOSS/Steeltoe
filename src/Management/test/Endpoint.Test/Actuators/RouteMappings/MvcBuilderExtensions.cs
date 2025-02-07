// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings;

internal static class MvcBuilderExtensions
{
    /// <summary>
    /// Replaces ASP.NET Core assembly scanning for controllers with only the provided ones.
    /// </summary>
    public static void HideControllersExcept(this IMvcBuilder builder, params Type[] controllerTypes)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(controllerTypes);

        var provider = new TestControllerProvider();

        foreach (Type controllerType in controllerTypes)
        {
            provider.AddController(controllerType);
        }

        builder.ConfigureApplicationPartManager(manager =>
        {
            RemoveExistingControllerFeatureProviders(manager);

            foreach (Assembly assembly in provider.ControllerAssemblies)
            {
                manager.ApplicationParts.Add(new AssemblyPart(assembly));
            }

            manager.FeatureProviders.Add(provider);
        });
    }

    private static void RemoveExistingControllerFeatureProviders(ApplicationPartManager manager)
    {
        IEnumerable<IApplicationFeatureProvider<ControllerFeature>> providers =
            manager.FeatureProviders.OfType<IApplicationFeatureProvider<ControllerFeature>>();

        foreach (IApplicationFeatureProvider<ControllerFeature> provider in providers.ToArray())
        {
            manager.FeatureProviders.Remove(provider);
        }
    }

    private sealed class TestControllerProvider : ControllerFeatureProvider
    {
        private readonly HashSet<Type> _allowedControllerTypes = [];

        internal HashSet<Assembly> ControllerAssemblies { get; } = [];

        public void AddController(Type controllerType)
        {
            _allowedControllerTypes.Add(controllerType);
            ControllerAssemblies.Add(controllerType.Assembly);
        }

        protected override bool IsController(TypeInfo typeInfo)
        {
            return _allowedControllerTypes.Contains(typeInfo);
        }
    }
}
