// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

#pragma warning disable SA1300 // ElementMustBeginWithUpperCaseLetter

namespace Steeltoe.Security.DataProtection.CredHub;

/// <summary>
/// Operations that can be allowed for an actor.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OperationPermissions
{
    read,
    write,
    delete,
    read_acl,
    write_acl
}
