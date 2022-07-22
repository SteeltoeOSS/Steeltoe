// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Info;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Management.Endpoint.Info.Contributor;

public class BuildInfoContributor : IInfoContributor
{
    private readonly Assembly _application;
    private readonly Assembly _steeltoe;

    public BuildInfoContributor()
    {
        _application = Assembly.GetEntryAssembly();
        _steeltoe = typeof(BuildInfoContributor).Assembly;
    }

    public void Contribute(IInfoBuilder builder)
    {
        builder.WithInfo("applicationVersionInfo", GetImportantDetails(_application));
        builder.WithInfo("steeltoeVersionInfo", GetImportantDetails(_steeltoe));

        // this is for Spring Boot Admin
        builder.WithInfo("build", new Dictionary<string, string> { { "version", _application.GetName().Version.ToString() } });
    }

    private Dictionary<string, string> GetImportantDetails(Assembly assembly)
    {
        return new Dictionary<string, string>
        {
            { "ProductName", assembly.GetName().Name },
            { "FileVersion", ((AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyFileVersionAttribute), false)).Version },
            { "ProductVersion", ((AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyInformationalVersionAttribute), false)).InformationalVersion }
        };
    }
}