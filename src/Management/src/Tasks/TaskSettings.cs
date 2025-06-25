// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;

namespace Steeltoe.Management.Tasks;

/// <summary>
/// Configuration settings for registered application tasks.
/// </summary>
internal sealed class TaskSettings
{
    // This type only exists to enable JSON schema documentation via ConfigurationSchemaAttribute.

    /// <summary>
    /// Gets or sets the name of the registered <see cref="IApplicationTask" /> to run.
    /// </summary>
    public string? RunTask { get; set; }
}
