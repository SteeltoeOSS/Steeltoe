// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Services;

public class Service
{
    [JsonIgnore]
    internal string Name { get; }

    [JsonPropertyName("scope")]
    public string Scope { get; }

    [JsonPropertyName("type")]
    public string Type { get; }

    [JsonPropertyName("resource")]
    public string AssemblyName { get; }

    [JsonPropertyName("dependencies")]
    public IList<string> Dependencies { get; }

    public Service(ServiceDescriptor descriptor)
    {
        ArgumentGuard.NotNull(descriptor);
        Name = descriptor.ServiceType.Name;
        Scope = descriptor.Lifetime.ToString();
        Type = descriptor.ImplementationInstance?.GetType()?.ToString() ?? descriptor.ServiceType.ToString();
        AssemblyName = descriptor.ServiceType.AssemblyQualifiedName ?? string.Empty;
        Dependencies = GetDependencies(descriptor) ?? new List<string>();
    }

    private IList<string>? GetDependencies(ServiceDescriptor descriptor)
    {
        var returnValue = new List<string>();
        ConstructorInfo[]? constructorInfos = descriptor.ImplementationType?.GetConstructors();

        if (constructorInfos != null)
        {
            ConstructorInfo? activatorUtilsInfo =
                Array.Find(constructorInfos, info => info.GetCustomAttribute(typeof(ActivatorUtilitiesConstructorAttribute)) != null);

            if (activatorUtilsInfo != null)
            {
                foreach (ParameterInfo parameterInfo in activatorUtilsInfo.GetParameters())
                {
                    returnValue.Add(parameterInfo.ToString());
                }
            }
            else
            {
                foreach (ConstructorInfo constructorInfo in constructorInfos)
                {
                    foreach (ParameterInfo parameterInfo in constructorInfo.GetParameters())
                    {
                        string parameterInfoString = parameterInfo.ToString();

                        if (!returnValue.Contains(parameterInfoString))
                        {
                            returnValue.Add(parameterInfo.ToString());
                        }
                    }
                }
            }
        }

        return returnValue;
    }
}
