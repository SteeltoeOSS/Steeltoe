// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Stream.Config;

public class BinderOptions : IBinderOptions
{
    private const bool InheritEnvironment_Default = true;
    private const bool DefaultCandidate_Default = true;

    public BinderOptions()
    {
    }

    public string ConfigureClass { get; set; }

    public string ConfigureAssembly { get; set; }

    public Dictionary<string, object> Environment { get; set; }

    public bool? InheritEnvironment { get; set; }

    public bool? DefaultCandidate { get; set; }

    bool IBinderOptions.InheritEnvironment => InheritEnvironment.Value;

    bool IBinderOptions.DefaultCandidate => DefaultCandidate.Value;

    internal void PostProcess()
    {
        if (Environment == null)
        {
            Environment = new Dictionary<string, object>();
        }

        if (!InheritEnvironment.HasValue)
        {
            InheritEnvironment = InheritEnvironment_Default;
        }

        if (!DefaultCandidate.HasValue)
        {
            DefaultCandidate = DefaultCandidate_Default;
        }
    }
}