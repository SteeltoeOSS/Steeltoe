// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Configuration;

public abstract class EndpointOptions
{
    private string? _path;

    internal virtual bool DefaultEnabled { get; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this endpoint is enabled.
    /// </summary>
    public virtual bool? Enabled { get; set; }

    /// <summary>
    /// Gets or sets the unique ID of this endpoint.
    /// </summary>
    public virtual string? Id { get; set; }

    /// <summary>
    /// Gets or sets the relative path at which this endpoint is exposed.
    /// </summary>
    public virtual string? Path
    {
        get
        {
            if (!string.IsNullOrEmpty(_path))
            {
                return _path;
            }

            return Id;
        }
        set => _path = value;
    }

    /// <summary>
    /// Gets or sets the permissions required to access this endpoint, when running on Cloud Foundry. Default value: Restricted.
    /// </summary>
    public Permissions RequiredPermissions { get; set; } = Permissions.Restricted;

    /// <summary>
    /// Gets the list of HTTP verbs that are allowed for this endpoint.
    /// </summary>
    public IList<string> AllowedVerbs { get; }

    protected EndpointOptions()
    {
        // ReSharper disable once VirtualMemberCallInConstructor
#pragma warning disable S1699 // Constructors should only call non-overridable methods
        AllowedVerbs = GetDefaultAllowedVerbs();
#pragma warning restore S1699 // Constructors should only call non-overridable methods
    }

    /// <summary>
    /// Gets the default list of HTTP verbs that are allowed for this endpoint.
    /// </summary>
    protected virtual IList<string> GetDefaultAllowedVerbs()
    {
        return new List<string>
        {
            "Get"
        };
    }

    /// <summary>
    /// Indicates whether this endpoint requires an exact match on the path.
    /// </summary>
    public virtual bool RequiresExactMatch()
    {
        return true;
    }
}
