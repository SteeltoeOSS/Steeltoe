// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Configuration;

public abstract class EndpointOptions
{
    private string? _path;

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
        get => _path ?? Id;
        set => _path = value;
    }

    /// <summary>
    /// Gets or sets the permissions required to access this endpoint, when running on Cloud Foundry. Default value: Restricted.
    /// </summary>
    public EndpointPermissions RequiredPermissions { get; set; } = EndpointPermissions.Restricted;

    /// <summary>
    /// Gets the list of HTTP verbs that are allowed for this endpoint.
    /// </summary>
    public IList<string> AllowedVerbs { get; private set; } = new List<string>();

    /// <summary>
    /// Gets the normalized set of allowed HTTP verbs: no duplicates, all uppercase, and no empty strings. An empty set indicates no verbs are allowed.
    /// </summary>
    internal HashSet<string> GetSafeAllowedVerbs()
    {
        return AllowedVerbs.Where(verb => verb.Length > 0).Select(verb => verb.ToUpperInvariant()).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    internal void ApplyDefaultAllowedVerbs()
    {
        AllowedVerbs = GetDefaultAllowedVerbs();
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
