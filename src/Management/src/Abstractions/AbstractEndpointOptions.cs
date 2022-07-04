// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management;

public abstract class AbstractEndpointOptions : IEndpointOptions
{
    protected bool? innerEnabled;

    protected bool? sensitive;

    protected string innerPath;

    protected AbstractEndpointOptions()
    {
    }

    protected AbstractEndpointOptions(string sectionName, IConfiguration config)
    {
        if (sectionName == null)
        {
            throw new ArgumentNullException(nameof(sectionName));
        }

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        var section = config.GetSection(sectionName);
        if (section != null)
        {
            section.Bind(this);
        }

        // These should not be set from configuration
        AllowedVerbs = null;
        ExactMatch = true;
    }

    public virtual bool? Enabled
    {
        get
        {
            return innerEnabled;
        }

        set
        {
            innerEnabled = value;
        }
    }

    public virtual string Id { get; set; }

    public virtual string Path
    {
        get
        {
            if (!string.IsNullOrEmpty(innerPath))
            {
                return innerPath;
            }

            return Id;
        }

        set
        {
            innerPath = value;
        }
    }

    public Permissions RequiredPermissions { get; set; } = Permissions.Undefined;

    public IManagementOptions Global { get; set; }

    public virtual bool IsAccessAllowed(Permissions permissions)
    {
        return permissions >= RequiredPermissions;
    }

    public virtual bool DefaultEnabled { get; } = true;

    public IEnumerable<string> AllowedVerbs { get; set; }

    public bool ExactMatch { get; set; } = true;
}
