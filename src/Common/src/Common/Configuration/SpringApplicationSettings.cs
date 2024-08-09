// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Steeltoe.Common.Configuration;

/// <summary>
/// Fallback configuration settings that describe this application.
/// </summary>
internal sealed class SpringApplicationSettings
{
    // This type only exists to enable JSON schema documentation via ConfigurationSchemaAttribute.

    /// <summary>
    /// Gets or sets the name of this application.
    /// </summary>
    public string? Name { get; set; }
}
