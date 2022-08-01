// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Stream.Config;

namespace Steeltoe.Stream.Binder;

public class BinderConfiguration
{
    private readonly IBinderOptions _options;

    public BinderConfiguration(string binderType, string binderAssemblyPath, IBinderOptions options)
    {
        ConfigureClass = binderType ?? throw new ArgumentNullException(nameof(binderType));
        ConfigureAssembly = binderAssemblyPath;
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string ConfigureClass { get; }

    public string ConfigureAssembly { get; }

    public string ResolvedAssembly { get; set; }

    public IDictionary<string, object> Properties
    {
        get
        {
            return _options.Environment;
        }
    }

    public bool IsInheritEnvironment
    {
        get
        {
            return _options.InheritEnvironment;
        }
    }

    public bool IsDefaultCandidate
    {
        get
        {
            return _options.DefaultCandidate;
        }
    }
}
