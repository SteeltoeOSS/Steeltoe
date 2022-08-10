// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Steeltoe.Common.Util;

namespace Steeltoe.Security.DataProtection.CredHub;

/// <summary>
/// Operations that can be allowed for an actor.
/// </summary>
[JsonConverter(typeof(SnakeCaseNoCapsEnumMemberJsonConverter))]
public enum OperationPermissions
{
    Read,
    Write,
    Delete,
    ReadAcl,
    WriteAcl
}
