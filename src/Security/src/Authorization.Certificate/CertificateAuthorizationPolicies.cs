// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Security.Authorization.Certificate;

/// <summary>
/// Use this class to refer to authorization policies for use with Cloud Foundry style certificates.
/// <example>
/// <code><![CDATA[
/// [Authorize(Policy = CertificateAuthorizationPolicies.SameOrg)]
/// [HttpGet]
/// public string AnyAppInTheOrgCanAccess()
/// {
///    return "Certificate is valid, client and server are in the same org.";
/// }
/// ]]>
/// </code>
/// </example>
/// </summary>
public static class CertificateAuthorizationPolicies
{
    /// <summary>
    /// Restrict access to application instances operating within the same organization (org).
    /// </summary>
    public const string SameOrg = "sameorg";

    /// <summary>
    /// Restrict access to application instances operating within the same space.
    /// </summary>
    public const string SameSpace = "samespace";
}
