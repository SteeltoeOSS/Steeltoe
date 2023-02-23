// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Management;
//Used to be abstract options
public class EndpointSharedOptions //: IEndpointOptions 
{
    protected bool? sensitive;

    protected string path;

    public virtual bool? Enabled { get; set; }

    public virtual string Id { get; set; }

    public virtual string Path
    {
        get
        {
            if (!string.IsNullOrEmpty(path))
            {
                return path;
            }

            return Id;
        }
        set => path = value;
    }

    public Permissions RequiredPermissions { get; set; } = Permissions.Undefined;

  //  public IManagementOptions Global { get; set; }

    public virtual bool DefaultEnabled { get; } = true;

    public IEnumerable<string> AllowedVerbs;  // Fields so they are not bound to configuration 

    public bool ExactMatch = true; // Fields so they are not bound to configuration

    public virtual bool IsAccessAllowed(Permissions permissions)
    {
        return permissions >= RequiredPermissions;
    }
}
