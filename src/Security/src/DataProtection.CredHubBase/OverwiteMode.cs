// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Text.Json.Serialization;

// ReSharper disable InconsistentNaming
#pragma warning disable SA1300 // ElementMustBeginWithUpperCaseLetter

namespace Steeltoe.Security.DataProtection.CredHub;

/// <summary>
/// Overwrite mode for existing credentials (https://credhub-api.cfapps.io/#overwriting-credential-values).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OverwiteMode
{
    [EnumMember(Value = "no-overwrite")]
    noOverwrite,
    overwrite,
    converge
}
