using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

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
        Name = descriptor.ServiceType.Name;
        Scope = descriptor.Lifetime.ToString();
        Type = descriptor.ImplementationInstance?.GetType()?.ToString() ?? descriptor.ServiceType.ToString();
        AssemblyName = descriptor.ServiceType.AssemblyQualifiedName ?? string.Empty;
        Dependencies = GetDependencies(descriptor) ?? new List<string>();
    }

    private IList<string>? GetDependencies(ServiceDescriptor descriptor)
    {
        var returnValue = new List<string>();
        var constructorInfos = descriptor.ImplementationType?.GetConstructors();
        if (constructorInfos != null)
        {
            ConstructorInfo? activatorUtilsInfo = constructorInfos.FirstOrDefault(ci => ci.GetCustomAttribute(typeof(ActivatorUtilitiesConstructorAttribute)) != null);
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
                        var parameterInfoString = parameterInfo.ToString();
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
