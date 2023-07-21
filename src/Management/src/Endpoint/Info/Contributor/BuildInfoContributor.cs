// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common;
using Steeltoe.Management.Info;

namespace Steeltoe.Management.Endpoint.Info.Contributor;

internal sealed class BuildInfoContributor : IInfoContributor
{
    private readonly Assembly _applicationAssembly;
    private readonly Assembly _steeltoeAssembly;

    public BuildInfoContributor()
    {
        _applicationAssembly = Assembly.GetEntryAssembly();
        _steeltoeAssembly = typeof(BuildInfoContributor).Assembly;
    }

    public Task ContributeAsync(IInfoBuilder builder, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(builder);

        builder.WithInfo("applicationVersionInfo", GetImportantDetails(_applicationAssembly));
        builder.WithInfo("steeltoeVersionInfo", GetImportantDetails(_steeltoeAssembly));

        // this is for Spring Boot Admin
        builder.WithInfo("build", new Dictionary<string, string>
        {
            { "version", _applicationAssembly.GetName().Version!.ToString() }
        });

        return Task.CompletedTask;
    }

    private Dictionary<string, string> GetImportantDetails(Assembly assembly)
    {
        string fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
        string productVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        return new Dictionary<string, string>
        {
            { "ProductName", assembly.GetName().Name },
            { "FileVersion", fileVersion },
            { "ProductVersion", productVersion }
        };
    }
}
