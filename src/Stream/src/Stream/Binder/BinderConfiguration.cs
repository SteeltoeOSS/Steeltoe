// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using Steeltoe.Stream.Configuration;

namespace Steeltoe.Stream.Binder;

public class BinderConfiguration
{
    private readonly IBinderOptions _options;

    public string ConfigureClass { get; }

    public string ConfigureAssembly { get; }

    public string ResolvedAssembly { get; set; }

    public IDictionary<string, object> Properties => _options.Environment;

    public bool IsInheritEnvironment => _options.InheritEnvironment;

    public bool IsDefaultCandidate => _options.DefaultCandidate;

    public BinderConfiguration(string binderType, string binderAssemblyPath, IBinderOptions options)
    {
        ArgumentGuard.NotNull(binderType);
        ArgumentGuard.NotNull(options);

        ConfigureClass = binderType;
        ConfigureAssembly = binderAssemblyPath;
        _options = options;
    }
}
