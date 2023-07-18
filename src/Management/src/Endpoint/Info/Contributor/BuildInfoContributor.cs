// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common;
using Steeltoe.Management.Info;

namespace Steeltoe.Management.Endpoint.Info.Contributor;

internal sealed class BuildInfoContributor : IInfoContributor
{
    private readonly Assembly _application;
    private readonly Assembly _steeltoe;

    public BuildInfoContributor()
    {
        _application = Assembly.GetEntryAssembly();
        _steeltoe = typeof(BuildInfoContributor).Assembly;
    }

    public Task ContributeAsync(IInfoBuilder builder)
    {
        ArgumentGuard.NotNull(builder);
        builder.WithInfo("applicationVersionInfo", GetImportantDetails(_application));
        builder.WithInfo("steeltoeVersionInfo", GetImportantDetails(_steeltoe));

        // this is for Spring Boot Admin
        builder.WithInfo("build", new Dictionary<string, string>
        {
            { "version", _application.GetName().Version.ToString() }
        });

        return Task.CompletedTask;
    }

    private Dictionary<string, string> GetImportantDetails(Assembly assembly)
    {
        return new Dictionary<string, string>
        {
            { "ProductName", assembly.GetName().Name },
            { "FileVersion", ((AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyFileVersionAttribute), false)).Version },
            {
                "ProductVersion",
                ((AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyInformationalVersionAttribute), false))
                .InformationalVersion
            }
        };
    }
}
