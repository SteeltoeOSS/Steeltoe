// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.Handler.Attributes;

/// <summary>
/// Attribute which indicates that a method parameter should be bound to a message header.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class HeaderAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the header to bind to.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the header binding is required.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the default value to use if header is missing.
    /// </summary>
    public string DefaultValue { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HeaderAttribute" /> class.
    /// </summary>
    /// <param name="name">
    /// the name of the request header to bind to.
    /// </param>
    /// <param name="defaultValue">
    /// the default value to use as a fallback.
    /// </param>
    /// <param name="required">
    /// is the header required.
    /// </param>
    public HeaderAttribute(string name = null, string defaultValue = null, bool required = true)
    {
        Name = name ?? string.Empty;
        Required = required;
        DefaultValue = defaultValue;
    }
}
