// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management;
public class EndpointOptionsBase : IEndpointOptions 
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

    public virtual bool DefaultEnabled { get; } = true;

    public virtual IEnumerable<string> AllowedVerbs { get; } = new List<string>() { "Get" };

    public virtual bool ExactMatch => true;

}
